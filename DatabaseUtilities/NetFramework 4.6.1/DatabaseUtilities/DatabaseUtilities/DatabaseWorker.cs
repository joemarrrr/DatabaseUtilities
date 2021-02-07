using DatabaseUtilities.Models;
using DatabaseUtilities.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUtilities
{
    public abstract class DatabaseWorker : IDisposable
    {
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private Converter _converter;
        private Queue<StaticParameter> _paramQueue;
        private string _connectioString;
        private SqlParameter[] _sqlParameters;
        protected Dictionary<string, object> OutputParameter;

        public DatabaseWorker(string connectionString)
        {
            try
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    _converter = new Converter();
                    var sqlBuilder = new SqlConnectionStringBuilder(connectionString);
                    sqlBuilder.MultipleActiveResultSets = true;
                    _connectioString = sqlBuilder.ToString();
                    _connection = new SqlConnection(_connectioString);
                    _connection.Open();
                    _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
                }
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception(e.Message);
            }
        }
        public DatabaseWorker(string serverName, string databaseName, string username, string password)
        {
            try
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    _converter = new Converter();
                    _connectioString = string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}; MultipleActiveResultSets=true;", serverName, databaseName, username, password);
                    _connection = new SqlConnection(_connectioString);
                    _connection.Open();
                    _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
                }
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception(e.Message);
            }

        }

        public void Dispose()
        {
            if (_connection != null || _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        protected void AddParameters(string paramName, object value, ParameterDirection direction, SqlDbType type, int size = 0)
        {
            if (_paramQueue == null)
                _paramQueue = new Queue<StaticParameter>();
            StaticParameter parameters = new StaticParameter(paramName, value, type, direction, size);
            _paramQueue.Enqueue(parameters);
        }

        protected void AddParameters(string paramName, ParameterDirection direction, SqlDbType type, int size = 0)
        {
            AddParameters(paramName, null, direction, type, size);
        }

        protected ProcessStatus<T> GetRecord<T>(string commandText, CommandType commandType, int commandTimeOut = 10800) where T : class
        {
            ProcessStatus<T> result = new ProcessStatus<T>();
            SqlDataReader reader = null;
            try
            {
                if (CheckConnection())
                {
                    result.IsSuccess = true;
                    SqlCommand command = _connection.CreateCommand();
                    command.CommandTimeout = commandTimeOut;
                    if (commandType == CommandType.Text)
                    {
                        command.Connection = _connection;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.Text;
                        reader = command.ExecuteReader();
                        while (reader.Read())
                            result.Result = _converter.ToClass<T>(reader);
                        reader.Close();
                    }
                    else
                    {
                        InitCallableStatements();
                        command.Connection = _connection;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(_sqlParameters);
                        reader = command.ExecuteReader();
                        while (reader.Read())
                            result.Result = _converter.ToClass<T>(reader);
                        reader.Close();
                        SetOutputParams(command.Parameters);
                    }
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ExceptionMessage = e.Message;
                result.FriendlyMessage = "Unable to select record, Please contact your administrator.";
                try
                {
                    _transaction.Rollback();
                }
                catch (Exception e1)
                {
                    result.ExceptionMessage = e1.Message;
                    result.FriendlyMessage = "Unable to rollback, Please contact your administrator.";
                }
                Dispose();
            }
            finally
            {
                if (reader != null && reader.IsClosed == false)
                    reader.Close();
            }
            return result;
        }
        protected ProcessStatus<ICollection<T>> GetRecords<T>(string commandText, CommandType commandType, int commandTimeOut = 10800) where T : class
        {
            ProcessStatus<ICollection<T>> result = new ProcessStatus<ICollection<T>>() { Result = new List<T>() };
            SqlDataReader reader = null;
            try
            {
                if (CheckConnection())
                {
                    result.IsSuccess = true;
                    SqlCommand command = _connection.CreateCommand();
                    command.CommandTimeout = commandTimeOut;
                    if (commandType == CommandType.Text)
                    {
                        command.Connection = _connection;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.Text;
                        reader = command.ExecuteReader();
                        while (reader.Read())
                            result.Result.Add(_converter.ToClass<T>(reader));
                        reader.Close();
                    }
                    else
                    {
                        InitCallableStatements();
                        command.Connection = _connection;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(_sqlParameters);
                        reader = command.ExecuteReader();
                        while (reader.Read())
                            result.Result.Add(_converter.ToClass<T>(reader));
                        reader.Close();
                        SetOutputParams(command.Parameters);
                    }
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ExceptionMessage = e.Message;
                result.FriendlyMessage = "Unable to select records, Please contact your administrator.";
                try
                {
                    _transaction.Rollback();
                }
                catch (Exception e1)
                {
                    result.ExceptionMessage = e1.Message;
                    result.FriendlyMessage = "Unable to rollback, Please contact your administrator.";
                }
                Dispose();
            }
            finally
            {
                if (reader != null && reader.IsClosed == false)
                    reader.Close();
            }
            return result;
        }
        protected ProcessStatus ModifyRecord(string commandText, CommandType commandType, int commandTimeOut = 10800)
        {
            ProcessStatus result = new ProcessStatus();
            try
            {
                if (CheckConnection())
                {
                    result.IsSuccess = true;
                    SqlCommand command = _connection.CreateCommand();
                    command.CommandTimeout = commandTimeOut;
                    if (commandType == CommandType.Text)
                    {
                        command.Connection = _connection;
                        command.Transaction = _transaction;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                        _transaction.Commit();
                    }
                    else
                    {
                        InitCallableStatements();
                        command.Connection = _connection;
                        command.Transaction = _transaction;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(_sqlParameters);
                        command.ExecuteNonQuery();
                        _transaction.Commit();
                        SetOutputParams(command.Parameters);
                    }
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ExceptionMessage = e.Message;
                result.FriendlyMessage = "Unable to insert record, Please contact your administrator.";
                try
                {
                    _transaction.Rollback();
                }
                catch (Exception e1)
                {
                    result.ExceptionMessage = e1.Message;
                    result.FriendlyMessage = "Unable to rollback, Please contact your administrator.";
                }
            }
            finally
            {
                Dispose();
            }
            return result;
        }
        protected ProcessStatus ModifyRecordWithTransaction(string commandText, CommandType commandType, int commandTimeOut = 10800)
        {
            ProcessStatus result = new ProcessStatus();
            try
            {
                if (CheckConnection())
                {
                    result.IsSuccess = true;
                    SqlCommand command = _connection.CreateCommand();
                    command.CommandTimeout = commandTimeOut;
                    if (commandType == CommandType.Text)
                    {
                        command.Connection = _connection;
                        command.Transaction = _transaction;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        InitCallableStatements();
                        command.Connection = _connection;
                        command.Transaction = _transaction;
                        command.CommandText = commandText;
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(_sqlParameters);
                        command.ExecuteNonQuery();
                        SetOutputParams(command.Parameters);
                    }
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ExceptionMessage = e.Message;
                result.FriendlyMessage = "Unable to insert record, Please contact your administrator.";
            }
            return result;
        }
        public ProcessStatus BulkInsert<T>(List<T> data, string destinationTable, bool isTransactional = false, int commandTimeOut = 10800) where T : class
        {
            ProcessStatus result = new ProcessStatus();
            try
            {
                if (CheckConnection())
                {
                    result.IsSuccess = true;
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, _transaction);
                    bulkCopy.DestinationTableName = destinationTable;
                    DataTable dt = _converter.ToDataTable<T>(data);
                    bulkCopy.WriteToServer(dt);
                    if (!isTransactional)
                        _transaction.Commit();
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ExceptionMessage = e.Message;
                result.FriendlyMessage = "Unable to insert bulk record, Please contact your administrator.";
                if (!isTransactional)
                {
                    try
                    {
                        _transaction.Rollback();
                    }
                    catch (Exception e1)
                    {
                        result.ExceptionMessage = e1.Message;
                        result.FriendlyMessage = "Unable to rollback, Please contact your administrator.";
                    }
                    finally
                    {
                        Dispose();
                    }
                }
            }
            return result;
        }
        protected ProcessStatus SaveChanges()
        {
            ProcessStatus result = new ProcessStatus();
            try
            {
                if (_connection != null && _connection.State == ConnectionState.Open &&
                        _transaction.Connection != null)
                {
                    _transaction.Commit();
                    result.IsSuccess = true;
                }
                else
                {
                    result.IsSuccess = false;
                    result.ExceptionMessage = "Unable to save transaction. Connection to database was close.";
                }
            }
            catch (Exception e)
            {
                try
                {
                    _transaction.Rollback();
                    throw new Exception(e.Message);
                }
                catch (Exception e1)
                {
                    throw new Exception(e1.Message);
                }
                finally
                {
                    Dispose();
                }
            }
            return result;
        }
        private void SetOutputParams(SqlParameterCollection parameters)
        {
            if (OutputParameter == null && parameters.Count > 0)
                OutputParameter = new Dictionary<string, object>();
            foreach (var item in parameters)
            {
                if (OutputParameter != null && parameters[item.ToString()].Direction == ParameterDirection.Output)
                    OutputParameter.Add(item.ToString(), parameters[item.ToString()].Value);
            }
        }
        public void ResetOutputParameter()
        {
            if (OutputParameter != null)
                OutputParameter = new Dictionary<string, object>();
        }
        private void InitCallableStatements()
        {
            List<SqlParameter> sqlParameter = new List<SqlParameter>();
            if (_paramQueue != null)
            {
                while (_paramQueue.Count > 0)
                {
                    StaticParameter parameter = _paramQueue.Dequeue();
                    if (parameter.ParameterDirection == ParameterDirection.Input)
                    {
                        sqlParameter.Add(new SqlParameter()
                        {
                            Direction = ParameterDirection.Input,
                            ParameterName = parameter.ParamName,
                            Value = parameter.ParamValue,
                            SqlDbType = parameter.ParamType,
                            Size = parameter.ParamSize
                        });
                    }
                    else
                    {
                        sqlParameter.Add(new SqlParameter()
                        {
                            Direction = ParameterDirection.Output,
                            ParameterName = parameter.ParamName,
                            Value = parameter.ParamValue,
                            SqlDbType = parameter.ParamType,
                            Size = parameter.ParamSize
                        });
                    }
                }
            }
            _sqlParameters = sqlParameter.ToArray();
        }
        private bool CheckConnection()
        {
            if (_connection == null || _connection.State == ConnectionState.Closed || _transaction.Connection == null)
                throw new Exception("Connection closed.");
            return true;
        }
    }
}
