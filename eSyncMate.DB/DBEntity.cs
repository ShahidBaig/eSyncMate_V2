// -----------------------------------------------------------------------
// <copyright file="DBEntity.cs" company="eSoftage">
// © 2014 eSoftage Pvt. Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace eSyncMate.DB
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DBEntity
    {
        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        [Nancy.Json.ScriptIgnore]
        [JsonIgnore]
        public DBConnector Connection
        {
            get
            {
                return m_Connection;
            }

            set
            {
                m_Connection = value;
            }
        }

        private DBConnector m_Connection;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        [Nancy.Json.ScriptIgnore]
        public Dictionary<string, string> Relacements
        {
            get
            {
                return m_Relacements;
            }

            set
            {
                m_Relacements = value;
            }
        }

        private Dictionary<string, string> m_Relacements;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        [Nancy.Json.ScriptIgnore]
        public List<string> ExtraFields
        {
            get
            {
                return m_ExtraFields;
            }

            set
            {
                m_ExtraFields = value;
            }
        }

        private List<string> m_ExtraFields;

        [JsonIgnore]
        public bool IsTemp { get; set; }
        [JsonIgnore]
        public List<string> Nullables { get; set; } = new List<string>();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DBEntity()
        {
            Connection = new DBConnector();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DBEntity(DBConnector connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DBEntity(string connectionString)
        {
            Connection = new DBConnector(connectionString);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public string PrepareQueries(object instance, string tableName, string endingPropertyName, ref string insertQueryStart, List<PropertyInfo> properties, string skipProperties = "")
        {
            DBEntity entity = (DBEntity)instance;
            string name = string.Empty;
            insertQueryStart = "INSERT INTO [" + tableName + "] (";
            foreach (PropertyInfo property in properties)
            {
                if (skipProperties.Contains(property.Name))
                    continue;

                name = GetFieldName(property.Name);
                insertQueryStart += "[" + name + "],";
                if (string.Compare(name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (entity.ExtraFields is object)
            {
                foreach (string field in entity.ExtraFields)
                    insertQueryStart += "[" + GetFieldName(field) + "],";
            }

            insertQueryStart = insertQueryStart.Substring(0, insertQueryStart.Length - 1);
            insertQueryStart += ") VALUES (";

            return insertQueryStart;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public string PrepareQueries(object instance, string tableName, string endingPropertyName, ref string insertQueryStart, List<DocumentFields> properties)
        {
            DBEntity entity = (DBEntity)instance;
            string name = string.Empty;
            insertQueryStart = "INSERT INTO [" + tableName + "] (";
            foreach (DocumentFields property in properties)
            {
                if (!property.IsProperty)
                {
                    continue;
                }

                name = GetFieldName(property.FieldName);
                insertQueryStart += "[" + name + "],";
                if (string.Compare(name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (entity.ExtraFields is object)
            {
                foreach (string field in entity.ExtraFields)
                    insertQueryStart += "[" + GetFieldName(field) + "],";
            }

            insertQueryStart = insertQueryStart.Substring(0, insertQueryStart.Length - 1);
            insertQueryStart += ") VALUES (";

            return insertQueryStart;
        }

        private string GetFieldName(string name)
        {
            if (Relacements is null || !Relacements.ContainsKey(name))
            {
                return name;
            }

            return Relacements[name];
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public string PrepareInsertQuery(object instance, string insertQueryStart, string endingPropertyName, List<PropertyInfo> properties, string skipProperties = "")
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            DBEntity entity = (DBEntity)instance;
            var l_Type = Declarations.FieldTypes.Boolean;
            foreach (PropertyInfo property in properties)
            {
                if (!string.IsNullOrEmpty(skipProperties) && skipProperties.Contains(property.Name))
                    continue;

                l_Type = GetFieldType(property, instance);
                if (l_Type == Declarations.FieldTypes.ByteArray)
                {
                    PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                }
                else
                {
                    PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                }

                l_Query += l_Param + ",";
                if (string.Compare(property.Name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (entity.ExtraFields is object)
            {
                PropertyInfo property;
                var type = instance.GetType();
                foreach (string field in entity.ExtraFields)
                {
                    property = type.GetProperty(field);
                    if (property is object)
                    {
                        l_Type = GetFieldType(property, instance);
                        if (l_Type == Declarations.FieldTypes.ByteArray)
                        {
                            PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                        }
                        else
                        {
                            PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                        }

                        l_Query += l_Param + ",";
                    }
                }
            }

            l_Query = l_Query.Substring(0, l_Query.Length - 1);
            l_Query = insertQueryStart + l_Query + ")";
            return l_Query;
        }

        /// <summary>
        /// Prepares Update Query Using All The Required Parameters.
        /// </summary>
        /// <l_Param name="instance">Instance of the Class for which The Update Query Is Prepared.</l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <l_Param name="endingPropertyName">Last Property in the Table</l_Param>
        /// <l_Param name="properties">List of Properties/Columns in the Table. </l_Param>
        /// <returns>Update Query</returns>
        public string PrepareUpdateQuery(object instance, string tableName, string primaryKey, string endingPropertyName, List<PropertyInfo> properties)
        {
            string l_Criteria = string.Empty;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Name = string.Empty;
            DBEntity l_Entity = (DBEntity)instance;
            var l_Type = Declarations.FieldTypes.Boolean;
            foreach (PropertyInfo l_Property in properties)
            {
                var property = l_Property;
                l_Name = GetFieldName(property.Name);
                l_Param = string.Empty;
                l_Type = GetFieldType(property, instance);
                if (l_Type == Declarations.FieldTypes.ByteArray)
                {
                    PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                }
                else
                {
                    PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                }

                if (string.IsNullOrEmpty(l_Criteria) && (l_Name.CompareTo(primaryKey) == 0 || ("Temp" + l_Name).CompareTo(primaryKey) == 0))
                {
                    if (("Temp" + l_Name).CompareTo(primaryKey) == 0)
                    {
                        var type = instance.GetType();
                        property = type.GetProperty(l_Name);
                        l_Param = string.Empty;
                        l_Type = GetFieldType(property, instance);
                        if (l_Type == Declarations.FieldTypes.ByteArray)
                        {
                            PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                        }
                        else
                        {
                            PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                        }

                        l_Query += "[" + l_Name + "] = " + l_Param + ",";
                        l_Name = "Temp" + l_Name;
                        property = type.GetProperty(l_Name);
                        l_Param = string.Empty;
                        l_Type = GetFieldType(property, instance);
                        if (l_Type == Declarations.FieldTypes.ByteArray)
                        {
                            PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                        }
                        else
                        {
                            PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                        }

                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                    else
                    {
                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                }
                else
                {
                    l_Query += "[" + l_Name + "] = " + l_Param + ",";
                }

                if (string.Compare(l_Name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (l_Entity.ExtraFields is object)
            {
                PropertyInfo property;
                var type = instance.GetType();
                foreach (string field in l_Entity.ExtraFields)
                {
                    property = type.GetProperty(field);
                    l_Name = GetFieldName(field);
                    l_Type = GetFieldType(property, instance);
                    if (l_Type == Declarations.FieldTypes.ByteArray)
                    {
                        PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                    }
                    else
                    {
                        PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                    }

                    if (string.IsNullOrEmpty(l_Criteria) && l_Name.CompareTo(primaryKey) == 0)
                    {
                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                    else
                    {
                        l_Query += "[" + l_Name + "] = " + l_Param + ",";
                    }
                }
            }

            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            l_Query = l_Query.Substring(0, l_Query.Length - 1);
            l_Query = "UPDATE [" + tableName + "] SET " + l_Query + l_Criteria;
            return l_Query;
        }

        /// <summary>
        /// Prepares Update Query Using All The Required Parameters.
        /// </summary>
        /// <l_Param name="instance">Instance of the Class for which The Update Query Is Prepared.</l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <l_Param name="endingPropertyName">Last Property in the Table</l_Param>
        /// <l_Param name="properties">List of Properties/Columns in the Table. </l_Param>
        /// <returns>Update Query</returns>
        public string PrepareUpdateQuery(object instance, string tableName, string primaryKey, string endingPropertyName, List<DocumentFields> properties)
        {
            string l_Criteria = string.Empty;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Name = string.Empty;
            DBEntity l_Entity = (DBEntity)instance;
            Document l_Document = (Document)instance;
            var l_Type = Declarations.FieldTypes.Boolean;
            foreach (DocumentFields l_Property in properties)
            {
                var property = l_Property;
                if (!property.IsProperty)
                {
                    continue;
                }

                l_Name = GetFieldName(property.FieldName);
                l_Param = string.Empty;
                l_Type = GetFieldType(property, instance);
                if (l_Type == Declarations.FieldTypes.ByteArray)
                {
                    PublicFunctions.FieldToParam("@" + property.FieldName, ref l_Param, l_Type);
                }
                else
                {
                    PublicFunctions.FieldToParam(property.FieldValue, ref l_Param, l_Type);
                }

                if (string.IsNullOrEmpty(l_Criteria) && (l_Name.CompareTo(primaryKey) == 0 || ("Temp" + l_Name).CompareTo(primaryKey) == 0))
                {
                    if (("Temp" + l_Name).CompareTo(primaryKey) == 0)
                    {
                        property = l_Document.GetProperty(l_Name);
                        l_Param = string.Empty;
                        l_Type = GetFieldType(property, instance);
                        if (l_Type == Declarations.FieldTypes.ByteArray)
                        {
                            PublicFunctions.FieldToParam("@" + property.FieldName, ref l_Param, l_Type);
                        }
                        else
                        {
                            PublicFunctions.FieldToParam(property.FieldValue, ref l_Param, l_Type);
                        }

                        l_Query += "[" + l_Name + "] = " + l_Param + ",";
                        l_Name = "Temp" + l_Name;
                        property = l_Document.GetProperty(l_Name);
                        l_Param = string.Empty;
                        l_Type = GetFieldType(property, instance);
                        if (l_Type == Declarations.FieldTypes.ByteArray)
                        {
                            PublicFunctions.FieldToParam("@" + property.FieldName, ref l_Param, l_Type);
                        }
                        else
                        {
                            PublicFunctions.FieldToParam(property.FieldValue, ref l_Param, l_Type);
                        }

                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                    else
                    {
                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                }
                else
                {
                    l_Query += "[" + l_Name + "] = " + l_Param + ",";
                }
            }

            if (l_Entity.ExtraFields is object)
            {
                PropertyInfo property;
                var type = instance.GetType();
                foreach (string field in l_Entity.ExtraFields)
                {
                    property = type.GetProperty(field);
                    l_Name = GetFieldName(field);
                    l_Type = GetFieldType(property, instance);
                    if (l_Type == Declarations.FieldTypes.ByteArray)
                    {
                        PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                    }
                    else
                    {
                        PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                    }

                    if (string.IsNullOrEmpty(l_Criteria) && l_Name.CompareTo(primaryKey) == 0)
                    {
                        l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
                    }
                    else
                    {
                        l_Query += "[" + l_Name + "] = " + l_Param + ",";
                    }
                }
            }

            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            l_Query = l_Query.Substring(0, l_Query.Length - 1);
            l_Query = "UPDATE [" + tableName + "] SET " + l_Query + l_Criteria;
            return l_Query;
        }

        private PropertyInfo GetProperty(List<PropertyInfo> properties, string name)
        {
            foreach (PropertyInfo l_Property in properties)
            {
                if ((l_Property.Name ?? "") == (name ?? ""))
                {
                    return l_Property;
                }
            }

            return default;
        }

        public string PrepareUpdateQueryNonPrimaryKey(object instance, string tableName, string endingPropertyName, List<PropertyInfo> properties)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Name = string.Empty;
            DBEntity l_Entity = (DBEntity)instance;
            var l_Type = Declarations.FieldTypes.Boolean;
            foreach (PropertyInfo property in properties)
            {
                l_Name = GetFieldName(property.Name);
                l_Param = string.Empty;
                l_Type = GetFieldType(property, instance);
                if (l_Type == Declarations.FieldTypes.ByteArray)
                {
                    PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                }
                else
                {
                    PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                }

                l_Query += "[" + l_Name + "] = " + l_Param + ",";
                if (string.Compare(l_Name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (l_Entity.ExtraFields is object)
            {
                PropertyInfo property;
                var type = instance.GetType();
                foreach (string field in l_Entity.ExtraFields)
                {
                    property = type.GetProperty(field);
                    l_Name = GetFieldName(field);
                    l_Type = GetFieldType(property, instance);
                    if (l_Type == Declarations.FieldTypes.ByteArray)
                    {
                        PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                    }
                    else
                    {
                        PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                    }

                    l_Query += "[" + l_Name + "] = " + l_Param + ",";
                }
            }

            l_Query = l_Query.Substring(0, l_Query.Length - 1);
            l_Query = "UPDATE [" + tableName + "] SET " + l_Query;
            return l_Query;
        }

        /// <summary>
        /// Prepares Update Query Using All The Required Parameters.
        /// </summary>
        /// <l_Param name="instance">Instance of the Class for which The Update Query Is Prepared.</l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <l_Param name="endingPropertyName">Last Property in the Table</l_Param>
        /// <l_Param name="properties">List of Properties/Columns in the Table. </l_Param>
        /// <returns>Update Query</returns>
        public string PrepareUpdateQuery(object instance, string tableName, List<string> primaryKeys, string endingPropertyName, List<PropertyInfo> properties)
        {
            string l_Criteria = string.Empty;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Name = string.Empty;
            DBEntity entity = (DBEntity)instance;
            var l_Type = Declarations.FieldTypes.Boolean;
            foreach (PropertyInfo l_Property in properties)
            {
                var property = l_Property;
                l_Name = GetFieldName(property.Name);
                l_Param = string.Empty;
                l_Type = GetFieldType(property, instance);
                if (l_Type == Declarations.FieldTypes.ByteArray)
                {
                    PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                }
                else
                {
                    PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                }

                if (primaryKeys.Contains(l_Name))
                {
                    if (string.IsNullOrEmpty(l_Criteria))
                    {
                        l_Criteria += " WHERE ";
                    }
                    else
                    {
                        l_Criteria += " AND ";
                    }

                    l_Criteria += " [" + l_Name + "] = " + l_Param;
                }
                else
                {
                    l_Query += "[" + l_Name + "] = " + l_Param + ",";
                }

                if (string.Compare(l_Name, endingPropertyName) == 0)
                {
                    break;
                }
            }

            if (entity.ExtraFields is object)
            {
                PropertyInfo property;
                var type = instance.GetType();
                foreach (string field in entity.ExtraFields)
                {
                    property = type.GetProperty(field);
                    l_Name = GetFieldName(field);
                    l_Type = GetFieldType(property, instance);
                    if (l_Type == Declarations.FieldTypes.ByteArray)
                    {
                        PublicFunctions.FieldToParam("@" + property.Name, ref l_Param, l_Type);
                    }
                    else
                    {
                        PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, l_Type);
                    }

                    if (primaryKeys.Contains(l_Name))
                    {
                        if (string.IsNullOrEmpty(l_Criteria))
                        {
                            l_Criteria = " WHERE [" + l_Name + "] = " + l_Param;
                        }
                        else
                        {
                            l_Criteria += " AND [" + l_Name + "] = " + l_Param;
                        }
                    }
                    else
                    {
                        l_Query += "[" + l_Name + "] = " + l_Param + ",";
                    }
                }
            }

            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            l_Query = l_Query.Substring(0, l_Query.Length - 1);
            l_Query = "UPDATE [" + tableName + "] SET " + l_Query + l_Criteria;
            return l_Query;
        }


        /// <summary>
        /// Prepares the Delete Query Using Required Parameters.
        /// </summary>
        /// <l_Param name="instance"> Instance of the Class for which The Update Query Is Prepared. </l_Param>
        /// <l_Param name="tableName"Name of the Table The Update Query Will Update.></l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Delete a Record From DB.</returns>
        public string PrepareDeleteQuery(object instance, string tableName, string primaryKey)
        {
            string l_Criteria = string.Empty;
            l_Criteria = PrepareCriteria(instance, primaryKey);
            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            return $"UPDATE [{tableName}] SET Status = 'DELETED' {l_Criteria}";
        }


        /// <summary>
        /// Prepares the Delete Query Using Required Parameters.
        /// </summary>
        /// <l_Param name="instance"> Instance of the Class for which The Update Query Is Prepared. </l_Param>
        /// <l_Param name="tableName"Name of the Table The Update Query Will Update.></l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Delete a Record From DB.</returns>
        public string PrepareDeleteQuery(object instance, string tableName, List<string> primaryKeys)
        {
            string l_Criteria = string.Empty;
            l_Criteria = PrepareCriteria(instance, primaryKeys);
            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            return "DELETE FROM [" + tableName + "]" + l_Criteria;
        }


        /// <summary>
        /// Prepares the Delete Query Using Required Parameters to Delete .
        /// </summary>
        /// <l_Param name="instance">Instance of the Class for which The Update Query Is Prepared.</l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Delete a Record From DB.</returns>
        public string PrepareDeleteAllQuery(string tableName, string primaryKey, string value)
        {
            string l_Criteria = string.Empty;
            l_Criteria = " WHERE [" + primaryKey + "] IN (" + value + ")";
            return "DELETE FROM [" + tableName + "]" + l_Criteria;
        }

        /// <summary>
        /// Prepares Query to Get All the data of a certain Column
        /// </summary>
        /// <l_Param name="instance"> Instance of the Class for which The Update Query Is Prepared. </l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Get the Object</returns>
        public string PrepareGetObjectQuery(object instance, string tableName, string primaryKey)
        {
            string l_Criteria = string.Empty;
            if (((DBEntity)instance).IsTemp)
            {
                tableName = tableName + "_Temp";
                object? l_Value = null;
                var l_Property = instance.GetType().GetProperty(primaryKey);
                if (l_Property is null)
                {
                    return string.Empty;
                }

                l_Value = l_Property.GetValue(instance);
                primaryKey = "Temp" + primaryKey;
                l_Property = instance.GetType().GetProperty(primaryKey);
                if (l_Property is null)
                {
                    return string.Empty;
                }

                l_Property.SetValue(instance, l_Value);
            }

            l_Criteria = PrepareCriteria(instance, primaryKey);
            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            return "SELECT * FROM [" + tableName + "]" + l_Criteria;
        }

        /// <summary>
        /// Prepares Query to Get All the data of a certain Column
        /// </summary>
        /// <l_Param name="instance"> Instance of the Class for which The Update Query Is Prepared. </l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Get the Object</returns>
        public string PrepareGetObjectQueryForLike(object instance, string tableName, string primaryKey)
        {
            string l_Criteria = string.Empty;
            if (((DBEntity)instance).IsTemp)
            {
                tableName = tableName + "_Temp";
                object l_Value = null;
                var l_Property = instance.GetType().GetProperty(primaryKey);
                if (l_Property is null)
                {
                    return string.Empty;
                }

                l_Value = l_Property.GetValue(instance);
                primaryKey = "Temp" + primaryKey;
                l_Property = instance.GetType().GetProperty(primaryKey);
                if (l_Property is null)
                {
                    return string.Empty;
                }

                l_Property.SetValue(instance, l_Value);
            }

            l_Criteria = PrepareCriteriaForLike(instance, primaryKey);
            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            return "SELECT * FROM [" + tableName + "]" + l_Criteria;
        }

        /// <summary>
        /// Prepares Query to Get All the data of a certain Column
        /// </summary>
        /// <l_Param name="instance"> Instance of the Class for which The Update Query Is Prepared. </l_Param>
        /// <l_Param name="tableName">Name of the Table The Update Query Will Update.</l_Param>
        /// <l_Param name="primaryKey">Primary Key of the Table.</l_Param>
        /// <returns>Query to Get the Object</returns>
        public string PrepareGetObjectQuery(object instance, string tableName, List<string> primaryKeys)
        {
            string l_Criteria = string.Empty;
            l_Criteria = PrepareCriteria(instance, primaryKeys);
            if (string.IsNullOrEmpty(l_Criteria))
            {
                return string.Empty;
            }

            return "SELECT * FROM [" + tableName + "]" + l_Criteria;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public void PopulateObject(object instance, DataTable data, List<PropertyInfo> properties, string endingPropertyName)
        {
            DataRow row = null;
            string name = string.Empty;
            if (data.Rows.Count < 1)
            {
                return;
            }

            row = data.Rows[0];
            PopulateObjectFromRow(instance, data, properties, endingPropertyName, row);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public void PopulateObject(object instance, DataTable data, List<DocumentFields> properties, string endingPropertyName)
        {
            DataRow row = null;
            string name = string.Empty;
            if (data.Rows.Count < 1)
            {
                return;
            }

            row = data.Rows[0];
            PopulateObjectFromRow(instance, data, properties, endingPropertyName, row);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public void PopulateObjectFromRow(object instance, DataTable data, List<PropertyInfo> properties, string endingPropertyName, DataRow row)
        {
            string l_Name = string.Empty;
            bool l_OtherProperties = false;
            foreach (PropertyInfo property in properties)
            {
                l_Name = GetFieldName(property.Name);
                if (data.Columns.Contains(l_Name))
                {
                    if (l_OtherProperties)
                    {
                        try
                        {
                            PopulateProperty(instance, l_Name, property, row);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    else
                    {
                        PopulateProperty(instance, l_Name, property, row);
                    }
                }

                if (string.Compare(l_Name, endingPropertyName) == 0)
                {
                    l_OtherProperties = true;
                }
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public void PopulateObjectFromRow(object instance, DataTable data, List<DocumentFields> properties, string endingPropertyName, DataRow row)
        {
            string name = string.Empty;
            foreach (DocumentFields property in properties)
            {
                name = GetFieldName(property.FieldName);
                if (string.Compare(name, endingPropertyName) == 0)
                {
                    if (data.Columns.Contains(name))
                    {
                        PopulateProperty(instance, name, property, row);
                    }

                    break;
                }

                if (data.Columns.Contains(name))
                {
                    PopulateProperty(instance, name, property, row);
                }
            }
        }

        private void PopulateProperty(object instance, string name, PropertyInfo property, DataRow row)
        {
            var type = GetFieldType(property, instance);
            switch (type)
            {
                case Declarations.FieldTypes.Boolean:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsBoolean(row[name], false), null);
                        break;
                    }

                case Declarations.FieldTypes.Byte:
                    {
                        property.SetValue(instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableByte:
                    {
                        property.SetValue(instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.Date:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsDateTime(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDate:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.String:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsString(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableString:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.Number:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsInteger(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableNumber:
                    {
                        property.SetValue(instance, Conversions.ToInteger(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NumberFloat:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableFloat:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], null), null);
                        break;
                    }

                case Declarations.FieldTypes.Decimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDecimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ByteArray:
                    {
                        if (Information.IsDBNull(row[name]))
                        {
                            property.SetValue(instance, null);
                        }
                        else
                        {
                            property.SetValue(instance, row[name], null);
                        }

                        break;
                    }

                case Declarations.FieldTypes.NotSupported:
                    {
                        break;
                    }
                default:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }
            }
        }

        private void PopulateProperty(object instance, string name, DocumentFields property, DataRow row)
        {
            var type = GetFieldType(property, instance);
            switch (type)
            {
                case Declarations.FieldTypes.Boolean:
                    {
                        property.SetValue((Document)instance, Conversions.ToBoolean(PublicFunctions.ConvertNull(row[name], false)), null);
                        break;
                    }

                case Declarations.FieldTypes.Byte:
                    {
                        property.SetValue((Document)instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableByte:
                    {
                        property.SetValue((Document)instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.Date:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDate:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.String:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableString:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.Number:
                    {
                        property.SetValue((Document)instance, Conversions.ToInteger(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableNumber:
                    {
                        property.SetValue((Document)instance, Conversions.ToInteger(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ShortNumber:
                    {
                        property.SetValue((Document)instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableShortNumber:
                    {
                        property.SetValue((Document)instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NumberFloat:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableFloat:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.Decimal:
                    {
                        property.SetValue((Document)instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDecimal:
                    {
                        property.SetValue((Document)instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ByteArray:
                    {
                        if (Information.IsDBNull(row[name]))
                        {
                            property.SetValue((Document)instance, null);
                        }
                        else
                        {
                            property.SetValue((Document)instance, row[name], null);
                        }

                        break;
                    }

                default:
                    {
                        property.SetValue((Document)instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private Declarations.FieldTypes GetFieldType(PropertyInfo property, object instance)
        {
            if (property.PropertyType == Type.GetType("System.String"))
            {
                if (Nullables.Contains(property.Name) && string.IsNullOrEmpty(Conversions.ToString(property.GetValue(instance, null))))
                {
                    return Declarations.FieldTypes.NullableString;
                }

                return Declarations.FieldTypes.String;
            }
            else if (property.PropertyType == Type.GetType("System.Guid"))
            {
                if (Nullables.Contains(property.Name) && Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(property.GetValue(instance, null), null, false)))
                {
                    return Declarations.FieldTypes.NullableString;
                }

                return Declarations.FieldTypes.String;
            }
            else if (property.PropertyType.IsGenericType)
            {
                var types = property.PropertyType.GetGenericArguments();
                if (types.Length == 0)
                {
                    return Declarations.FieldTypes.Number;
                }

                if (property.PropertyType.Name == "Nullable`1")
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return Declarations.FieldTypes.NullableString;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return Declarations.FieldTypes.NullableDate;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return Declarations.FieldTypes.NullableByte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return Declarations.FieldTypes.NullableNumber;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return Declarations.FieldTypes.NullableShortNumber;
                    }

                    return Declarations.FieldTypes.NullableFloat;
                }
                else
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return Declarations.FieldTypes.String;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return Declarations.FieldTypes.Date;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return Declarations.FieldTypes.Byte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return Declarations.FieldTypes.Number;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return Declarations.FieldTypes.ShortNumber;
                    }

                    return Declarations.FieldTypes.NumberFloat;
                }
            }
            else if (property.PropertyType == Type.GetType("System.DateTime"))
            {
                return Declarations.FieldTypes.Date;
            }
            else if (property.PropertyType == Type.GetType("System.Boolean"))
            {
                return Declarations.FieldTypes.Boolean;
            }
            else if (property.PropertyType == Type.GetType("System.Byte[]"))
            {
                return Declarations.FieldTypes.ByteArray;
            }
            else if (property.PropertyType == Type.GetType("System.Byte"))
            {
                return Declarations.FieldTypes.Byte;
            }
            else if (property.PropertyType == Type.GetType("System.Decimal"))
            {
                return Declarations.FieldTypes.Decimal;
            }
            else if (property.PropertyType == Type.GetType("System.Int32") || property.PropertyType == Type.GetType("System.Int64") || property.PropertyType == Type.GetType("System.UInt32") || property.PropertyType == Type.GetType("System.UInt64"))
            {
                return Declarations.FieldTypes.Number;
            }
            else if (property.PropertyType == Type.GetType("System.Int16") || property.PropertyType == Type.GetType("System.UInt16"))
            {
                return Declarations.FieldTypes.ShortNumber;
            }

            return Declarations.FieldTypes.NotSupported;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private Declarations.FieldTypes GetFieldType(DocumentFields property, object instance)
        {
            if (property.FieldType == "varchar")
            {
                if (Nullables.Contains(property.FieldName) && string.IsNullOrEmpty(Conversions.ToString(property.FieldValue)))
                {
                    return Declarations.FieldTypes.NullableString;
                }

                return Declarations.FieldTypes.String;
            }
            else if (property.FieldType == "uniqueidentifier")
            {
                if (Nullables.Contains(property.FieldName) && Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(property.FieldValue, null, false)))
                {
                    return Declarations.FieldTypes.NullableString;
                }

                return Declarations.FieldTypes.String;
            }
            else if (property.FieldType == "datetime")
            {
                return Declarations.FieldTypes.Date;
            }
            else if (property.FieldType == "bit")
            {
                return Declarations.FieldTypes.Boolean;
            }
            else if (property.FieldType == "byte()")
            {
                return Declarations.FieldTypes.ByteArray;
            }
            else if (property.FieldType == "tinyint")
            {
                return Declarations.FieldTypes.Byte;
            }
            else if (property.FieldType == "money" || property.FieldType == "real")
            {
                return Declarations.FieldTypes.Decimal;
            }
            else if (property.FieldType == "int")
            {
                return Declarations.FieldTypes.Number;
            }

            return Declarations.FieldTypes.NumberFloat;
        }

        /// <summary>
        /// Prepares the Criteria for Any CRUD Operation
        /// </summary>
        /// <l_Param name="instance"></l_Param>
        /// <l_Param name="primaryKey"></l_Param>
        private string PrepareCriteria(object instance, string primaryKey)
        {
            string l_Criteria = string.Empty;
            string l_Param = string.Empty;
            var property = instance.GetType().GetProperty(primaryKey);
            if (property is null)
            {
                var l_Property = ((Document)instance).GetProperty(primaryKey);
                PublicFunctions.FieldToParam(l_Property.FieldValue, ref l_Param, GetFieldType(l_Property, instance));
            }
            else
            {
                PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, GetFieldType(property, instance));
            }

            l_Criteria = " WHERE [" + primaryKey + "] = " + l_Param;
            return l_Criteria;
        }

        /// <summary>
        /// Prepares the Criteria for Any CRUD Operation
        /// </summary>
        /// <l_Param name="instance"></l_Param>
        /// <l_Param name="primaryKey"></l_Param>
        private string PrepareCriteriaForLike(object instance, string primaryKey)
        {
            string l_Criteria = string.Empty;
            string l_Param = string.Empty;
            var property = instance.GetType().GetProperty(primaryKey);
            if (property is null)
            {
                var l_Property = ((Document)instance).GetProperty(primaryKey);
                PublicFunctions.FieldToParam(l_Property.FieldValue, ref l_Param, GetFieldType(l_Property, instance));
            }
            else
            {
                PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, GetFieldType(property, instance));
            }

            l_Param = l_Param.Replace("'", "");
            l_Criteria = " WHERE [" + primaryKey + "] LIKE " + "'%" + l_Param + "%'";
            return l_Criteria;
        }

        private string PrepareCriteria(object instance, List<string> primaryKeys)
        {
            string l_Criteria = string.Empty;
            string l_Param = string.Empty;
            PropertyInfo property = null;
            var instanceType = instance.GetType();
            property = instanceType.GetProperty(primaryKeys[0]);
            if (property is null)
            {
                foreach (string key in primaryKeys)
                {
                    var l_Property = ((Document)instance).GetProperty(key);
                    PublicFunctions.FieldToParam(l_Property.FieldValue, ref l_Param, GetFieldType(l_Property, instance));
                    if (string.IsNullOrEmpty(l_Criteria))
                    {
                        l_Criteria = " WHERE [" + key + "] = " + l_Param;
                    }
                    else
                    {
                        l_Criteria += " AND [" + key + "] = " + l_Param;
                    }
                }
            }
            else
            {
                foreach (string key in primaryKeys)
                {
                    property = instanceType.GetProperty(key);
                    PublicFunctions.FieldToParam(property.GetValue(instance, null), ref l_Param, GetFieldType(property, instance));
                    if (string.IsNullOrEmpty(l_Criteria))
                    {
                        l_Criteria = " WHERE [" + key + "] = " + l_Param;
                    }
                    else
                    {
                        l_Criteria += " AND [" + key + "] = " + l_Param;
                    }
                }
            }

            return l_Criteria;
        }

        public void SetProperty(string p_PropertyName, object p_PropertyValue)
        {
            var l_InstanceType = GetType();
            var l_Properties = l_InstanceType.GetProperties();
            var l_Property = l_InstanceType.GetProperty(p_PropertyName);
            if (l_Property is object)
            {
                PopulateProperty(this, l_Property, p_PropertyValue);
            }
        }

        private void PopulateProperty(object instance, PropertyInfo property, object value)
        {
            var type = GetFieldType(property, instance);
            switch (type)
            {
                case Declarations.FieldTypes.Boolean:
                    {
                        property.SetValue(instance, Conversions.ToBoolean(value), null);
                        break;
                    }

                case Declarations.FieldTypes.Byte:
                    {
                        property.SetValue(instance, Conversions.ToByte(value), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableByte:
                    {
                        property.SetValue(instance, Conversions.ToByte(value), null);
                        break;
                    }

                case Declarations.FieldTypes.Date:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDate:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.String:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.NullableString:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.Number:
                    {
                        property.SetValue(instance, Conversions.ToInteger(value), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableNumber:
                    {
                        property.SetValue(instance, Conversions.ToInteger(value), null);
                        break;
                    }

                case Declarations.FieldTypes.ShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(value), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(value), null);
                        break;
                    }

                case Declarations.FieldTypes.NumberFloat:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.NullableFloat:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }

                case Declarations.FieldTypes.Decimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(value), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDecimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(value), null);
                        break;
                    }

                case Declarations.FieldTypes.ByteArray:
                    {
                        if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(value, null, false)))
                        {
                            property.SetValue(instance, null);
                        }
                        else
                        {
                            property.SetValue(instance, value, null);
                        }

                        break;
                    }

                default:
                    {
                        property.SetValue(instance, value, null);
                        break;
                    }
            }
        }

        public virtual Result GetDetails(object p_DocumentNo)
        {
            MethodInfo l_Method = null;
            PropertyInfo l_Property = null;
            Type l_DocumentType = null;
            var l_Result = Result.GetFailureResult();
            try
            {
                l_DocumentType = GetType();
                l_Method = l_DocumentType.GetMethod("GetObject", new Type[] { Type.GetType("System.Int32") });
                if (l_Method is null)
                {
                    l_Method = l_DocumentType.GetMethod("GetObject", new Type[] { Type.GetType("System.Int16") });
                    if (l_Method is null)
                    {
                        l_Method = l_DocumentType.GetMethod("GetObject", new Type[] { Type.GetType("System.String") });
                        if (l_Method is null)
                        {
                            l_Method = l_DocumentType.GetMethod("GetObject", new Type[] { Type.GetType("System.Byte") });
                            if (l_Method is null)
                            {
                                l_Method = l_DocumentType.GetMethod("GetObject", new Type[] { Type.GetType("System.Int32"), Type.GetType("System.Boolean"), Type.GetType("System.Boolean") });
                                l_Result = (Result)l_Method.Invoke(this, new[] { Conversions.ToInteger(p_DocumentNo), false, (object)true });
                            }
                            else
                            {
                                l_Result = (Result)l_Method.Invoke(this, new[] { (object)Conversions.ToByte(p_DocumentNo) });
                            }
                        }
                        else
                        {
                            l_Result = (Result)l_Method.Invoke(this, new[] { p_DocumentNo });
                        }
                    }
                    else
                    {
                        l_Result = (Result)l_Method.Invoke(this, new[] { (object)Convert.ToInt16(p_DocumentNo) });
                    }
                }
                else
                {
                    l_Result = (Result)l_Method.Invoke(this, new[] { (object)Conversions.ToInteger(p_DocumentNo) });
                }
            }
            catch (Exception ex)
            {
            }

            return l_Result;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static void PopulateObjectFromRow(object instance, DataTable data, DataRow row)
        {
            PropertyInfo[] properties = instance.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (data.Columns.Contains(property.Name))
                {
                    try
                    {
                        DBEntity.PopulateProperty(instance, property, row);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        public static void PopulateProperty(object instance, PropertyInfo property, DataRow row)
        {
            var type = DBEntity.GetFieldType(instance, property);
            string name = property.Name;

            switch (type)
            {
                case Declarations.FieldTypes.Boolean:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsBoolean(row[name], false), null);
                        break;
                    }

                case Declarations.FieldTypes.Byte:
                    {
                        property.SetValue(instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableByte:
                    {
                        property.SetValue(instance, Conversions.ToByte(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.Date:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsDateTime(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDate:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], DateTime.MinValue), null);
                        break;
                    }

                case Declarations.FieldTypes.String:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsString(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableString:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], string.Empty), null);
                        break;
                    }

                case Declarations.FieldTypes.Number:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNullAsInteger(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableNumber:
                    {
                        property.SetValue(instance, Conversions.ToInteger(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableShortNumber:
                    {
                        property.SetValue(instance, Conversions.ToShort(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NumberFloat:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableFloat:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], null), null);
                        break;
                    }

                case Declarations.FieldTypes.Decimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.NullableDecimal:
                    {
                        property.SetValue(instance, Conversions.ToDecimal(PublicFunctions.ConvertNull(row[name], 0)), null);
                        break;
                    }

                case Declarations.FieldTypes.ByteArray:
                    {
                        if (Information.IsDBNull(row[name]))
                        {
                            property.SetValue(instance, null);
                        }
                        else
                        {
                            property.SetValue(instance, row[name], null);
                        }

                        break;
                    }

                case Declarations.FieldTypes.NotSupported:
                    {
                        break;
                    }
                default:
                    {
                        property.SetValue(instance, PublicFunctions.ConvertNull(row[name], 0), null);
                        break;
                    }
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static Declarations.FieldTypes GetFieldType(object instance, PropertyInfo property)
        {
            if (property.PropertyType == Type.GetType("System.String"))
            {
                return Declarations.FieldTypes.String;
            }
            else if (property.PropertyType == Type.GetType("System.Guid"))
            {
                return Declarations.FieldTypes.String;
            }
            else if (property.PropertyType.IsGenericType)
            {
                var types = property.PropertyType.GetGenericArguments();
                if (types.Length == 0)
                {
                    return Declarations.FieldTypes.Number;
                }

                if (property.PropertyType.Name == "Nullable`1")
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return Declarations.FieldTypes.NullableString;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return Declarations.FieldTypes.NullableDate;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return Declarations.FieldTypes.NullableByte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return Declarations.FieldTypes.NullableNumber;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return Declarations.FieldTypes.NullableShortNumber;
                    }

                    return Declarations.FieldTypes.NullableFloat;
                }
                else
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return Declarations.FieldTypes.String;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return Declarations.FieldTypes.Date;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return Declarations.FieldTypes.Byte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return Declarations.FieldTypes.Number;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return Declarations.FieldTypes.ShortNumber;
                    }

                    return Declarations.FieldTypes.NumberFloat;
                }
            }
            else if (property.PropertyType == Type.GetType("System.DateTime"))
            {
                return Declarations.FieldTypes.Date;
            }
            else if (property.PropertyType == Type.GetType("System.Boolean"))
            {
                return Declarations.FieldTypes.Boolean;
            }
            else if (property.PropertyType == Type.GetType("System.Byte[]"))
            {
                return Declarations.FieldTypes.ByteArray;
            }
            else if (property.PropertyType == Type.GetType("System.Byte"))
            {
                return Declarations.FieldTypes.Byte;
            }
            else if (property.PropertyType == Type.GetType("System.Decimal"))
            {
                return Declarations.FieldTypes.Decimal;
            }
            else if (property.PropertyType == Type.GetType("System.Int32") || property.PropertyType == Type.GetType("System.Int64") || property.PropertyType == Type.GetType("System.UInt32") || property.PropertyType == Type.GetType("System.UInt64"))
            {
                return Declarations.FieldTypes.Number;
            }
            else if (property.PropertyType == Type.GetType("System.Int16") || property.PropertyType == Type.GetType("System.UInt16"))
            {
                return Declarations.FieldTypes.ShortNumber;
            }

            return Declarations.FieldTypes.NotSupported;
        }
    }
}