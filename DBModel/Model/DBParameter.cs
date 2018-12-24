using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
namespace DBModel
{
   public class DBParameter
    {
        private int Direction = 1;
        public object Value { get; set; }
        public int DbType { get; set; }
        public int ParameterDirection { get { return Direction; } set { Direction = value; } }
    }
}
