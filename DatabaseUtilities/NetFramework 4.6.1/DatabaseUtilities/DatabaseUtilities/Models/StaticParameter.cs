using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUtilities.Models
{
    public class StaticParameter
    {
        public StaticParameter(string sp_paramName, object sp_paramValue, SqlDbType sp_paramType, ParameterDirection
                sp_paramDirection, int paramSize)
        {
            ParamName = sp_paramName;
            if (sp_paramValue != null)
            {
                ParamValue = sp_paramValue;
            }
            ParamType = sp_paramType;
            ParameterDirection = sp_paramDirection;
            ParamSize = paramSize;
        }
        public string ParamName { get; set; }
        public object ParamValue { get; set; }
        public SqlDbType ParamType { get; set; }
        public int ParamSize { get; set; }
        public ParameterDirection ParameterDirection { get; set; }
    }
}
