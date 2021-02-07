using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DatabaseUtilities.Utilities
{
    public class Converter
    {
        public DataTable ToDataTable<T>(List<T> objectValue) where T : class
        {
            DataTable dt = new DataTable();
            T returnedObject = Activator.CreateInstance<T>();
            List<PropertyInfo> modelProperties = returnedObject.GetType().GetProperties().OrderBy(p => p.MetadataToken).ToList();
            List<string> colNames = new List<string>();
            foreach (PropertyInfo item in modelProperties)
            {
                colNames.Add(item.Name);
                dt.Columns.Add(item.Name, Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType);
            }

            foreach (var item in objectValue)
            {
                List<object> temp = new List<object>();
                foreach (string colName in colNames)
                    temp.Add(item.GetType().GetProperty(colName).GetValue(item));
                dt.Rows.Add(temp.ToArray());
            }
            return dt;

        }
        public T ToClass<T>(SqlDataReader reader) where T : class
        {
            var schemaTable = reader.GetSchemaTable();
            List<string> colNames = new List<string>();
            foreach (DataRow row in schemaTable.Rows)
                colNames.Add(row["ColumnName"].ToString());

            T returnedObject = Activator.CreateInstance<T>();
            List<PropertyInfo> modelProperties = returnedObject.GetType().GetProperties().OrderBy(p => p.MetadataToken).ToList();

            for (int i = 0; i < modelProperties.Count; i++)
            {
                bool hasSameName = false;
                foreach (string colName in colNames)
                {
                    if (colName.ToLower() == modelProperties[i].Name.ToLower())
                    {
                        hasSameName = true;
                        modelProperties[i].SetValue(returnedObject, Convert.ChangeType(Convert.IsDBNull(reader[colName]) ? ConvertNullValue(modelProperties[i].PropertyType) : reader[colName], modelProperties[i].PropertyType), null);
                        var test = ConvertNullValue(modelProperties[i].PropertyType);
                        break;
                    }
                }
                if (hasSameName == false)
                    modelProperties[i].SetValue(returnedObject, ConvertNullValue(modelProperties[i].PropertyType), null);

            }
            return returnedObject;

        }
        private object ConvertNullValue(Type propertyType)
        {
            object output;
            if (Nullable.GetUnderlyingType(propertyType) != null)
                output = null;
            else if (propertyType == typeof(byte))
                output = 0;
            else if (propertyType == typeof(sbyte))
                output = 0;
            else if (propertyType == typeof(short))
                output = 0;
            else if (propertyType == typeof(ushort))
                output = 0;
            else if (propertyType == typeof(int))
                output = 0;
            else if (propertyType == typeof(uint))
                output = 0;
            else if (propertyType == typeof(long))
                output = 0;
            else if (propertyType == typeof(ulong))
                output = 0;
            else if (propertyType == typeof(float))
                output = 0;
            else if (propertyType == typeof(double))
                output = 0;
            else if (propertyType == typeof(decimal))
                output = 0;
            else if (propertyType == typeof(char))
                output = null;
            else if (propertyType == typeof(Boolean))
                output = false;
            else if (propertyType == typeof(bool))
                output = false;
            else if (propertyType == typeof(string))
                output = null;
            else if (propertyType == typeof(DateTime))
                output = DateTime.Now;
            else
                output = null;
            return output;
        }
    }
}
