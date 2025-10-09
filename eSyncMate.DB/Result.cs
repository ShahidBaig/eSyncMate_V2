using System;
using System.Data;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{
    public class Result : IDisposable
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public int ErrorLevel { get; set; }
        public int ErrorType { get; set; }
        public string ErrorID { get; set; }
        public string MessageID { get; set; }
        public bool IsSuccess { get; set; }
        public string DocumentNo { get; set; }
        public string DocumentKey { get; set; }
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
        public string P5 { get; set; }
        public string P6 { get; set; }
        public DataTable Data { get; set; }

        public static Result GetSuccessResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.SuccessCodes.OperationCompleted;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = true;
            return l_Result;
        }

        public static Result GetFailureResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.OperationFailed;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetInvalidSKUResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.InvalidSKU;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetNoRecordResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.NoRecord;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetDuplicateRecordResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.DuplicateDocument;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetPreSaveResult(string p_ErrorCode)
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.PreSaveError;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            l_Result.ErrorID = p_ErrorCode;
            return l_Result;
        }

        public static Result GetDocumentAssociationResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ExceptionCodes.DeleteDocumentAssociation;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetPriceCategoryResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.CheckPriceCategory;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = true;
            return l_Result;
        }

        public static Result GetDefaultAddress()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.CheckDefaultAddress;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = true;
            return l_Result;
        }

        public static Result CheckCustomerAddresses()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.DeleteCustomerAddress;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = true;
            return l_Result;
        }

        public static Result GetDuplicateCarrierCodeResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.DuplicateCarrierCode;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public static Result GetDesignIDLengthResult()
        {
            var l_Result = new Result();
            l_Result.Code = (int)Declarations.ErrorCodes.DesignIDLength;
            l_Result.Description = PublicFunctions.GetMessageDescription(l_Result.Code);
            l_Result.IsSuccess = false;
            return l_Result;
        }

        public void Populate(DataTable p_Data)
        {
            DocumentNo = string.Empty;
            {
                var withBlock = this;
                withBlock.Code = (int)Declarations.ErrorCodes.OperationFailed;
                withBlock.Description = PublicFunctions.GetMessageDescription(withBlock.Code);
                withBlock.IsSuccess = false;
                withBlock.ErrorLevel = -1;
            }

            foreach (DataRow l_Row in p_Data.Rows)
            {
                foreach (DataColumn l_Column in p_Data.Columns)
                {
                    if (string.Compare(l_Column.ColumnName, "Success", true) == 0)
                    {
                        IsSuccess = Conversions.ToBoolean(PublicFunctions.ConvertNull(l_Row[l_Column], false));
                    }
                    else if (string.Compare(l_Column.ColumnName, "Code", true) == 0)
                    {
                        Code = Conversions.ToInteger(PublicFunctions.ConvertNull(l_Row[l_Column], 0));
                    }
                    else if (string.Compare(l_Column.ColumnName, "Description", true) == 0 || string.Compare(l_Column.ColumnName, "ErrorMessage", true) == 0 || string.Compare(l_Column.ColumnName, "Message", true) == 0 || string.Compare(l_Column.ColumnName, "ErrDesc", true) == 0)
                    {
                        if (String.IsNullOrEmpty(Description) || Description == PublicFunctions.GetMessageDescription((int)Declarations.ErrorCodes.OperationFailed))
                        {
                            Description = Conversions.ToString(PublicFunctions.ConvertNull(l_Row[l_Column], string.Empty));
                        }
                    }
                    else if (string.Compare(l_Column.ColumnName, "MessageLevel", true) == 0)
                    {
                        ErrorLevel = Conversions.ToInteger(PublicFunctions.ConvertNull(l_Row[l_Column], 0));
                    }
                    else if (string.Compare(l_Column.ColumnName, "ErrorNumber", true) == 0)
                    {
                        ErrorID = Conversions.ToString(PublicFunctions.ConvertNull(l_Row[l_Column], string.Empty));
                    }
                    else if (string.Compare(l_Column.ColumnName, "MessageID", true) == 0)
                    {
                        MessageID = Conversions.ToString(PublicFunctions.ConvertNull(l_Row[l_Column], string.Empty));
                    }
                    else if (string.Compare(l_Column.ColumnName, "ErrorState", true) == 0 || string.Compare(l_Column.ColumnName, "MessageType", true) == 0)
                    {
                        ErrorType = Conversions.ToInteger(PublicFunctions.ConvertNull(l_Row[l_Column], 0));
                    }
                    else if (string.IsNullOrEmpty(DocumentNo) && (LikeOperator.LikeString(l_Column.ColumnName, "Temp*No", CompareMethod.Binary) || LikeOperator.LikeString(l_Column.ColumnName, "Temp*ID", CompareMethod.Binary) || LikeOperator.LikeString(l_Column.ColumnName, "*No", CompareMethod.Binary) || LikeOperator.LikeString(l_Column.ColumnName, "*ID", CompareMethod.Binary) || l_Column.ColumnName == "TempAccount"))
                    {
                        DocumentNo = Conversions.ToString(PublicFunctions.ConvertNull(l_Row[l_Column], string.Empty));
                        DocumentKey = l_Column.ColumnName;
                    }
                    else if (string.Compare(l_Column.ColumnName, "P1", true) == 0)
                    {
                        P1 = Conversions.ToString(PublicFunctions.ConvertNull(l_Row[l_Column], string.Empty));
                    }
                }

                if (IsSuccess == false && ErrorLevel == 0 && (p_Data.Columns.Contains("ErrorLevel") || p_Data.Columns.Contains("MessageLevel")))
                {
                    IsSuccess = true;
                    Code = (int)Declarations.SuccessCodes.OperationCompleted;
                }

                if (!(p_Data.Columns.Contains("ErrorLevel") || p_Data.Columns.Contains("MessageLevel")))
                {
                    ErrorLevel = 0;
                }

                break;
            }

            Data = p_Data.Copy();
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Data is object)
                    {
                        Data.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                // TODO: set large fields to null.
            }

            disposedValue = true;
        }

        // TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        // Protected Overrides Sub Finalize()
        // ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        // Dispose(False)
        // MyBase.Finalize()
        // End Sub

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}