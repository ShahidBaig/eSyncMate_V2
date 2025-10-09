// -----------------------------------------------------------------------
// <copyright file="AccountType.cs" company="eSoftage">
// © 2014 eSoftage Pvt. Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;

namespace eSyncMate.DB
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public interface IDBEntity
    {
        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        bool GetList(string Criteria, string Fields, ref DataTable Data, string OrderBy = "");

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        bool GetViewList(string Criteria, string Fields, ref DataTable Data, string OrderBy = "");

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        int GetMax();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result GetObjectFromQuery(string Query, bool isOnlyObject = false);

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result GetObject();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result GetObjectOnly();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result SaveNew();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result Modify();

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        Result Delete();
    }
}