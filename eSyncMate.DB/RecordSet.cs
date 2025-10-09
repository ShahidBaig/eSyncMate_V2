using System;
using System.ComponentModel;
using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{
    public class RecordSet : DataTable
    {

        #region Declarations
        private int m_CurrentRowIndex = -1;
        private bool m_EOF;
        private bool m_BOF;
        private DataTable m_Schema;
        // Private m_BookMark As Integer = -1
        #endregion

        public RecordSet()
        {
            m_CurrentRowIndex = 0;
            m_BOF = false;
            m_EOF = false;
        }

        public DataTable Schema
        {
            get
            {
                return m_Schema;
            }

            set
            {
                if (m_Schema is object)
                {
                    m_Schema.Dispose();
                }

                m_Schema = value;
            }
        }

        public int RecordCount
        {
            get
            {
                if ((Filter ?? "") == (string.Empty ?? ""))
                {
                    return Rows.Count;
                }

                return DefaultView.Count;
            }
        }

        public bool EOF
        {
            get
            {
                return m_EOF;
                // If Me.Filter = String.Empty  Then
                // If m_CurrentRowIndex = Me.Rows.Count Then
                // Return True
                // End If
                // Else
                // If m_CurrentRowIndex = Me.DefaultView.Count Then
                // Return True
                // End If
                // End If

                // Return False
            }

            set
            {
                m_EOF = value;
                if (value)
                {
                    if ((Filter ?? "") == (string.Empty ?? ""))
                    {
                        m_CurrentRowIndex = Rows.Count;
                    }
                    else
                    {
                        m_CurrentRowIndex = DefaultView.Count;
                    }
                }
            }
        }

        public bool BOF
        {
            get
            {
                return m_BOF;
                // If m_CurrentRowIndex < 0 Then
                // Return True
                // End If

                // Return False
            }

            set
            {
                if (value)
                {
                    m_CurrentRowIndex = -1;
                }

                m_BOF = value;
            }
        }

        public string Sort
        {
            get
            {
                return DefaultView.Sort;
            }

            set
            {
                DefaultView.Sort = value;
            }
        }

        public void MoveFirst()
        {
            m_CurrentRowIndex = 0;
            m_BOF = false;
            m_EOF = false;
            if ((Filter ?? "") == (string.Empty ?? ""))
            {
                if (Rows.Count == 0)
                {
                    m_CurrentRowIndex = -1;
                }
            }
            else if (DefaultView.Count == 0)
            {
                m_CurrentRowIndex = -1;
            }

            if (m_CurrentRowIndex < 0)
            {
                m_BOF = true;
                m_EOF = true;
            }
        }

        public void MoveLast()
        {
            m_BOF = false;
            m_EOF = false;
            if ((Filter ?? "") == (string.Empty ?? ""))
            {
                m_CurrentRowIndex = Rows.Count - 1;
            }
            else
            {
                m_CurrentRowIndex = DefaultView.Count - 1;
            }

            if (m_CurrentRowIndex < 0)
            {
                m_BOF = true;
                m_EOF = true;
            }
        }

        public void MoveNext()
        {
            m_CurrentRowIndex += 1;
            m_EOF = false;
            if ((Filter ?? "") == (string.Empty ?? ""))
            {
                if (m_CurrentRowIndex == Rows.Count)
                {
                    m_EOF = true;
                }
            }
            else if (m_CurrentRowIndex == DefaultView.Count)
            {
                m_EOF = true;
            }
        }

        public void MovePrevious()
        {
            m_CurrentRowIndex -= 1;
            if (m_CurrentRowIndex < 0)
            {
                m_BOF = true;
            }
            else
            {
                m_BOF = false;
            }
        }

        public DataRow CurrentRow()
        {
            DataRow CurrentRowRet = default;
            CurrentRowRet = null;
            if (Rows.Count == 0 || m_CurrentRowIndex < 0 || m_CurrentRowIndex >= Rows.Count)
            {
                return CurrentRowRet;
            }

            if (string.IsNullOrEmpty(Filter) || string.IsNullOrEmpty(Sort))
            {
                CurrentRowRet = DefaultView[m_CurrentRowIndex].Row;
            }
            else
            {
                CurrentRowRet = Rows[m_CurrentRowIndex];
            }

            return CurrentRowRet;
        }

        public FieldValue FieldByString(string p_Column)
        {
            FieldValue FieldByStringRet = default;
            FieldByStringRet = null;
            if (!Columns.Contains(p_Column))
            {
                return FieldByStringRet;
            }

            return this[Columns.IndexOf(p_Column)];
        }

        public int BookMark
        {
            get
            {
                return m_CurrentRowIndex;
            }

            set
            {
                m_CurrentRowIndex = value;
            }
        }

        public string Filter
        {
            get
            {
                return DefaultView.RowFilter;
            }

            set
            {
                DefaultView.RowFilter = value;
                m_CurrentRowIndex = 0;
                m_BOF = false;
                m_EOF = false;
                if (DefaultView.Count == 0)
                {
                    m_BOF = true;
                    m_EOF = true;
                    m_CurrentRowIndex = -1;
                }
            }
        }

        public int AbsolutePosition
        {
            get
            {
                return m_CurrentRowIndex;
            }

            set
            {
                m_CurrentRowIndex = value;
            }
        }

        public void Move(int p_BoolMark, int p_Direction = 0)
        {
            m_CurrentRowIndex = p_BoolMark;
            // m_BookMark = p_BoolMark
        }

        public void Find(string p_Criteria)
        {
            var l_Rows = Select(p_Criteria, string.Empty);
            m_BOF = false;
            m_EOF = false;
            if (l_Rows.Length <= 0)
            {
                m_CurrentRowIndex = -1;
                m_BOF = true;
                m_EOF = true;
                return;
            }

            m_CurrentRowIndex = Rows.IndexOf(l_Rows[0]);
        }

        public FieldValue this[int p_Index]
        {
            get
            {
                FieldValue FieldsRet = default;
                FieldValue l_Val;
                FieldsRet = null;
                if (!(p_Index >= 0 && p_Index < Columns.Count))
                {
                    return default;
                }

                if ((Filter ?? "") == (string.Empty ?? "") && (Sort ?? "") == (string.Empty ?? ""))
                {
                    if (!(m_CurrentRowIndex >= 0 && m_CurrentRowIndex < Rows.Count))
                    {
                        return default;
                    }
                }
                else if (!(m_CurrentRowIndex >= 0 && m_CurrentRowIndex < DefaultView.Count))
                {
                    return default;
                }

                l_Val = (FieldValue)Columns[p_Index].ExtendedProperties["Field"];
                if (l_Val is null)
                {
                    l_Val = new FieldValue();
                    l_Val.m_Name = Columns[p_Index].ColumnName;
                    l_Val.m_Type = Columns[p_Index].DataType;
                    l_Val.DefinedSize = Columns[p_Index].MaxLength;
                    if (m_Schema is object)
                    {
                        var l_Rows = m_Schema.Select("ColumnName = '" + l_Val.m_Name + "'");
                        if (l_Rows.Length > 0)
                        {
                            l_Val.DefinedSize = Conversions.ToInteger(PublicFunctions.ConvertNull(l_Rows[0]["ColumnSize"], 0));
                        }
                    }

                    Columns[p_Index].ExtendedProperties["Field"] = l_Val;
                }

                if ((Filter ?? "") == (string.Empty ?? "") && (Sort ?? "") == (string.Empty ?? ""))
                {
                    l_Val.Value = Rows[m_CurrentRowIndex][p_Index];
                }
                else
                {
                    l_Val.Value = DefaultView[m_CurrentRowIndex][p_Index];
                }

                return l_Val;
            }
        }

        public void Close()
        {
        }

        public new void ImportRows(DataRow p_DataRow)
        {
            BOF = false;
            ImportRow(p_DataRow);
        }
    }

    [DefaultProperty("Value")]
    public class FieldValue : object
    {
        private object m_Value;
        public Type m_Type;
        public string m_Name = string.Empty;
        private int m_DefinedSize = 0;

        public Type Type
        {
            get
            {
                return m_Type;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public object Value
        {
            get
            {
                return m_Value;
            }

            set
            {
                m_Value = value;
            }
        }

        public int DefinedSize
        {
            get
            {
                return m_DefinedSize;
            }

            set
            {
                m_DefinedSize = value;
            }
        }
    }
}