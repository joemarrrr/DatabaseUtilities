using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseUtilities.Models
{
    public class ProcessStatus<T>
    {
        public T Result { get; set; }
        public bool IsSuccess { get; set; }
        public string ExceptionMessage { get; set; }
        public string FriendlyMessage { get; set; }

        public void CreateException(Exception error, string methodName)
        {
            this.ExceptionMessage = string.Format("[%s] : %s", methodName, error.ToString());
            this.IsSuccess = false;
        }

    }
    public class ProcessStatus
    {
        public bool IsSuccess { get; set; }
        public string ExceptionMessage { get; set; }
        public string FriendlyMessage { get; set; }

        public void CreateException(Exception error, string methodName)
        {
            this.ExceptionMessage = string.Format("[%s] : %s", methodName, error.ToString());
            this.IsSuccess = false;
        }

    }
}
