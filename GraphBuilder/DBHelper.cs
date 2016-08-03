using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DBGraph
{
    public sealed class DBHelper
    {
        public string DBConnectionString = null;
        public string Request = null;

        public DBHelper()
        {
            try
            {
                DBConnectionString = DBGraph.Properties.Settings.Default.DBConnectionString;
                Request = DBGraph.Properties.Settings.Default.Request;
            }
            catch (Exception)
            {
                //throw;
            }
        }

        public void LoadData(ref Dictionary<string, DBEntryDescriptor> DBObjects)
        {
            using (SqlConnection cnn = new SqlConnection(DBConnectionString))
            {
                SqlCommand cmd = new SqlCommand(Request, cnn);
                cmd.CommandTimeout = cnn.ConnectionTimeout;
                try
                {
                    cnn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        string currentIdFrom = dr["IdFrom"].ToString();
                        string currentIdTo = (dr["IdTo"] != DBNull.Value ? dr["IdTo"].ToString() : string.Empty);
                        if (dr["IdFrom"] != DBNull.Value && !DBObjects.ContainsKey(currentIdFrom))
                        {
                            DBEntryDescriptor dbobjectFrom = new DBEntryDescriptor();
                            dbobjectFrom.Name = dr["NameFrom"].ToString();
                            dbobjectFrom.Id = currentIdFrom.ToString();
                            dbobjectFrom.DbType = GetTypeName(dr["TypeFrom"].ToString());
                            dbobjectFrom.Shape = GetShapeByTypeCode(dr["TypeFrom"].ToString());
                            dbobjectFrom.Members = new Dictionary<string, string>();
                            try
                            {
                                if (dr["UrlFrom"] != null && dr["UrlFrom"] != DBNull.Value)
                                    dbobjectFrom.Url = dr["UrlFrom"].ToString();
                                if (dr["DescriptionFrom"] != null && dr["DescriptionFrom"] != DBNull.Value)
                                    dbobjectFrom.Description = dr["DescriptionFrom"].ToString();
                            }
                            catch (Exception)
                            {
                            }
                            DBObjects.Add(currentIdFrom, dbobjectFrom);
                        }
                        DBEntryDescriptor dbobjectTo;
                        if (dr["IdTo"] != DBNull.Value)
                        {
                            if (!DBObjects.ContainsKey(currentIdTo))
                            {
                                dbobjectTo = new DBEntryDescriptor();
                                dbobjectTo.Name = dr["NameTo"].ToString();
                                dbobjectTo.Id = dr["IdTo"].ToString();
                                dbobjectTo.DbType = GetTypeName(dr["TypeTo"].ToString());
                                dbobjectTo.Shape = GetShapeByTypeCode(dr["TypeTo"].ToString());
                                dbobjectTo.Members = new Dictionary<string, string>();
                                try
                                {
                                    if (dr["UrlTo"] != null && dr["UrlTo"] != DBNull.Value)
                                        dbobjectTo.Url = dr["UrlTo"].ToString();
                                    if (dr["DescriptionTo"] != null && dr["DescriptionTo"] != DBNull.Value)
                                        dbobjectTo.Description = dr["DescriptionTo"].ToString();
                                }
                                catch (Exception)
                                {
                                }
                                DBObjects.Add(currentIdTo, dbobjectTo);
                            }
                            else
                            {
                                dbobjectTo = DBObjects[currentIdTo];
                            }
                            if (!dbobjectTo.Members.ContainsKey(currentIdFrom.ToString()))
                                dbobjectTo.Members.Add(currentIdFrom.ToString(), dr["Actions"].ToString());
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                finally
                {
                    if (cnn.State != System.Data.ConnectionState.Closed)
                        cnn.Close();
                }
            }
        }

        private string GetTypeName(string typeCode)
        {
            string result;
            switch (typeCode.ToUpper().Trim())
	        {   
                case "C":
                    result="CHECK constraint"; //CHECK
                    break;
                case "D":
                    result="Default or DEFAULT constraint"; //DEFAULT_CONSTRAINT
                    break;
                case "F":
                    result="FOREIGN KEY constraint"; //FOREIGN_KEY_CONSTRAINT
                    break;
                case "L":
                    result="Log";
                    break;
                case "FN":
                    result="Scalar function"; //SQL_SCALAR_FUNCTION
                    break;
                case "IF":
                    result="Inlined table-function";
                    break;
                case "IT":
                    result="INTERNAL_TABLE";
                    break;
                case "P":
                    result="Stored procedure"; //SQL_STORED_PROCEDURE
                    break;
                case "K":
                    result="PRIMARY KEY or UNIQUE constraint";
                    break;
                case "PK": //xtype = PK: PRIMARY KEY constraint (type is K)
                    result="PRIMARY KEY constraint"; //PRIMARY_KEY_CONSTRAINT
                    break;
                case "RF":
                    result = "Replication filter stored procedure";
                    break;
                case "S":
                    result="System table"; //SYSTEM_TABLE
                    break;
                case "SN":
                    result="SYNONYM";
                    break;
                case "SQ":
                    result="SERVICE_QUEUE";
                    break;
                case "TF":
                    result="Table function";
                    break;
                case "TR":
                    result="Trigger"; //SQL_TRIGGER
                    break;
                case "U":
                    result = "User table"; //USER_TABLE
                    break;
                case "UQ": //UNIQUE constraint (type is K)
                    result="UNIQUE constraint (type is K)"; //UNIQUE_CONSTRAINT
                    break;
                case "V":
                    result="View";
                    break;
                case "X":
                    result="Extended stored procedure";
                    break;
		        default:
                    result = typeCode.ToUpper().Trim(); // "UNKNOWN";
                    break;
	        }
            return result;

        }
        private string GetShapeByTypeCode(string typeCode)
        {
            string result;
            switch (typeCode.ToUpper().Trim())
            {
                case "C"://CHECK
                case "D"://DEFAULT_CONSTRAINT
                case "F"://FOREIGN_KEY_CONSTRAINT
                case "K":
                case "PK": //xtype = PK: PRIMARY KEY constraint (type is K)
                case "UQ": //UNIQUE constraint (type is K)
                    result = "diamond"; 
                    break;
                case "L":
                    result = "trapezoid2";
                    break;
                case "FN"://SQL_SCALAR_FUNCTION
                case "IF":
                case "TF":
                case "ONTIME DEFECT":
                    result = "ellipse"; 
                    break;
                case "IT":
                case "U"://USER_TABLE
                case "S"://SYSTEM_TABLE
                    result = "roundrectangle";
                    break;
                case "P":
                case "ONTIME INCIDENT":
                    result = "octagon"; //SQL_STORED_PROCEDURE
                    break;
                case "RF":
                case "ONTIME CHANGE REQUEST":
                    result = "hexagon";
                    break;
                case "SN":
                case "ONTIME PROJECT":
                    result = "parallelogram"; 
                    break;
                case "SQ":
                case "ONTIME TASK WORKFLOW":
                case "ONTIME INCIDENT WORKFLOW":
                case "ONTIME CHANGE REQUEST WORKFLOW":
                case "ONTIME DEFECT WORKFLOW":
                    result = "trapezoid";
                    break;
                case "TR":
                case "ONTIME TASK":
                    result = "triangle"; //SQL_TRIGGER
                    break;
                case "V":
                case "ONTIME WORKFLOW STEP":
                    result = "roundrectangle";
                    break;
                case "X":
                    result = "octagon";
                    break;
                default:
                    result = "ellipse";
                    if (typeCode.Replace(" ","").ToUpper().Contains("DATASET"))
                    {
                        result = "diamond";
                    }
                    break;
            }
            return result;

        }
    }

    public struct DBEntryDescriptor
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        private string url;
        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private string dbType;
        public string DbType
        {
            get { return dbType; }
            set { dbType = value; }
        }

        private string shape;
        public string Shape
        {
            get { return shape; }
            set { shape = value; }
        }

        private Dictionary<string, string> members;
        public Dictionary<string, string> Members
        {
            get { return members; }
            set { members = value; }
        }

        /*public Group()
        {
            members = new List<string>();
        }*/
    }

}
