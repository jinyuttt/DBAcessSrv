using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DBClient
{
   public class Parameter
    {
        private ParameterDirection Direction = ParameterDirection.Input;
        public object Value { get; set; }
        public DbType DbType { get; set; }
        public ParameterDirection ParamDirection { get { return Direction; } set { Direction = value; } }

        public string Name { get; set; }
    }
}
