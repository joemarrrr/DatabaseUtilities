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
            foreach (PropertyInfo item2 in modelProperties)
            {
                colNames.Add(item2.Name);
                dt.Columns.Add(item2.Name, Nullable.GetUnderlyingType(item2.PropertyType) ?? item2.PropertyType);
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
                        Type t = Nullable.GetUnderlyingType(modelProperties[i].PropertyType) ?? modelProperties[i].PropertyType;
                        object value = (Convert.IsDBNull(reader[colName])) ? null : Convert.ChangeType(reader[colName], t);
                        modelProperties[i].SetValue(returnedObject, value, null);
                        break;
                    }
                }
                if (hasSameName == false)
                {
                    Type t = Nullable.GetUnderlyingType(modelProperties[i].PropertyType) ?? modelProperties[i].PropertyType;
                    modelProperties[i].SetValue(returnedObject, null, null);
                }

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
                output = 0.0;
            else if (propertyType == typeof(double))
                output = 0.0;
            else if (propertyType == typeof(decimal))
                output = 0.0;
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
