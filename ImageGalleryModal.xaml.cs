using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;

namespace Mercader;

public partial class ImageGalleryModal : ContentPage
{
    private readonly ArticuloBase _articulo;
    private readonly IImageStorageService _imageStorageService;

    public ImageGalleryModal(ArticuloBase articulo, IImageStorageService imageStorageService)
    {
        InitializeComponent();
        _articulo = articulo;
        _imageStorageService = imageStorageService;
        LoadImages();
    }

    private void LoadImages()
    {
        // Slot 0
        UpdateSlot(0, Image0, Placeholder0, DeleteBtn0);
        // Slot 1
        UpdateSlot(1, Image1, Placeholder1, DeleteBtn1);
        // Slot 2
        UpdateSlot(2, Image2, Placeholder2, DeleteBtn2);
        // Slot 3
        UpdateSlot(3, Image3, Placeholder3, DeleteBtn3);
    }

    private void UpdateSlot(int slot, Image image, Frame placeholder, Button deleteBtn)
    {
        var path = _articulo.GetImagePath(slot);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            image.Source = ImageSource.FromFile(path);
            image.IsVisible = true;
            placeholder.IsVisible = false;
            deleteBtn.IsVisible = true;
        }
        else
        {
            image.Source = null;
            image.IsVisible = false;
            placeholder.IsVisible = true;
            deleteBtn.IsVisible = false;
        }
    }

    private async void OnSlotTapped(object sender, TappedEventArgs e)
    {
        if (_articulo.GetFirstEmptySlot() == -1)
        {
            await DisplayAlert("Imágenes", "Ya tenés 4 imágenes. Eliminá una para agregar otra.", "OK");
            return;
        }

        var action = await DisplayActionSheet("Agregar imagen", "Cancelar", null, "Tomar foto", "Elegir de la galería");

        if (action == "Tomar foto")
        {
            await TakePhotoAsync();
        }
        else if (action == "Elegir de la galería")
        {
            await PickFromGalleryAsync();
        }
    }

    private async Task TakePhotoAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso requerido", "Se necesita permiso de cámara para tomar fotos.", "OK");
                return;
            }

            var result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Tomar foto"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                var path = await _imageStorageService.CompressAndSaveAsync(stream);
                if (path != null)
                {
                    var slot = _articulo.GetFirstEmptySlot();
                    _articulo.SetImageAtSlot(slot, path);
                    LoadImages();
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }

    private async Task PickFromGalleryAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Seleccionar imagen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                var path = await _imageStorageService.CompressAndSaveAsync(stream);
                if (path != null)
                {
                    var slot = _articulo.GetFirstEmptySlot();
                    _articulo.SetImageAtSlot(slot, path);
                    LoadImages();
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteImageClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.StyleId, out int slot))
        {
            var path = _articulo.GetImagePath(slot);
            if (!string.IsNullOrWhiteSpace(path))
            {
                var confirm = await DisplayAlert("Eliminar imagen", "¿Seguro que querés eliminar esta imagen?", "Eliminar", "Cancelar");
                if (confirm)
                {
                    await _imageStorageService.DeleteFileAsync(path);
                    _articulo.ClearSlot(slot);
                    LoadImages();
                }
            }
        }
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
