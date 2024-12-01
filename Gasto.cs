﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public class Gasto
    {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("Descripcion")]
        public required string Descripcion { get; set; }

        [Column("Monto")]
        public decimal Monto { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }
    }
}
