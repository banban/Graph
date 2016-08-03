using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DBGraph
{
    public enum GraphTypes { Undefined = 0, All = 1, AllAndUser = 2, UserOnly = 3 }
    public enum ShapeTypes { Figure = 1, Image = 2, Picture = 3 }

    public class YedHelper
    {
        public string YedOutputFilePath = null;
        public YedHelper()
        {
            try
            {
                YedOutputFilePath = Properties.Settings.Default.YedOutputFilePath;
            }
            catch (Exception)
            {
                //throw;
            }
        }

        public void Build(Dictionary<string, DBEntryDescriptor> dbObjects)
        {
            GraphBuilder graph = new GraphBuilder();
            XElement xmlRootSectionGraph = new XElement(graph.GraphNS + "graph",
                    new XAttribute("id", "G"), //GRAPH
                    new XAttribute("edgedefault", "directed"),
                    new XAttribute("label", "Database Graph")
            );
            XElement AppGroup = graph.CreateGroupElement(new DBEntryDescriptor
            {
                Id = "o0",
                Name = "Applications",
                Description = "this group controls releshionships between applications and database objects"
            });
            XElement AppGroupSectionGraph = (XElement)AppGroup.LastNode;

            XElement DBGroup = graph.CreateGroupElement(new DBEntryDescriptor
            {
                Id = "u0",
                Name = "Database",
                Description = "this group controls releshionships between database objects"
            });
            XElement DBGroupSectionGraph = (XElement)DBGroup.LastNode;
            //CultureInfo ci = new CultureInfo("en-EN"); ;
            bool AppGroupHasElements = false, DBGroupHasElements = false;
            #region objects
            List<string> dbTypeNames = new List<string>();
            
            List<DBEntryDescriptor> objectList = new List<DBEntryDescriptor>();
            foreach (KeyValuePair<string, DBEntryDescriptor> dbObjectPair in dbObjects)
            {
                objectList.Add(dbObjectPair.Value);
            }

            for (int i = 0; i < objectList.Count; i++)
            {
                string dbTypeId = "u0";
                DBEntryDescriptor dbObject = objectList[i];
                if ((!dbTypeNames.Contains(dbObject.DbType)) && (!String.IsNullOrEmpty(dbObject.DbType)))
                {
                    dbTypeId = dbTypeId + "::dp" + (dbTypeNames.Count + 1).ToString();
                    dbTypeNames.Add(dbObject.DbType);
                    XElement dbTypeGroup = new XElement(
                        graph.CreateGroupElement(new DBEntryDescriptor
                        {
                            Id = dbTypeId,
                            Name = dbObject.DbType,
                            Description = dbObject.DbType
                        }));
                    XElement dbTypeGroupSectionGraph = (XElement)dbTypeGroup.LastNode;

                    graph.ShapeType = ShapeTypes.Image;
                    for (int j = 0; j < objectList.Count; j++)
                    {
                        DBEntryDescriptor objectInfo = objectList[j];
                        if (objectInfo.DbType == dbObject.DbType)
                        {
                            XElement entryElement = graph.CreateNodeElement(objectInfo);
                            if (entryElement != null)
                            {
                                //objectInfo.Id = dbTypeId + "::n" + objectInfo.Id;
                                objectInfo.Path = dbTypeId + "::n" + objectInfo.Id;
                                entryElement.SetAttributeValue("id", objectInfo.Path);

                                dbTypeGroupSectionGraph.Add(entryElement);
                                dbObjects[objectInfo.Id] = objectInfo;
                                objectList[j] = objectInfo;
                            }
                        }
                    }
                    graph.ShapeType = ShapeTypes.Figure;
                    string groupvalue = dbTypeGroup.Value;
                    if (groupvalue.StartsWith("MESSAGEOBJECT")
                            || groupvalue.StartsWith("UNIT")
                            || groupvalue.StartsWith("PROJECT")
                            || groupvalue.StartsWith("REPLICATION")
                            || groupvalue.StartsWith("MENU")
                        )
                    {
                        AppGroupSectionGraph.Add(dbTypeGroup);
                        AppGroupHasElements = true;
                    }
                    if (groupvalue.StartsWith("HEAT ")
                            || groupvalue.StartsWith("ONTIME ")
                        )
                    {
                        xmlRootSectionGraph.Add(dbTypeGroup);
                    }
                    else
                    {
                        DBGroupSectionGraph.Add(dbTypeGroup);
                        DBGroupHasElements = true;
                    }

                    //var groupName = dbTypeGroup.XPathEvaluate("node/data[@key='d3'/ProxyAutoBoundsNode/Realizers/GroupNode/NodeLabel[@modelName='internal']]");
                    //if (groupName != null)
                    //{
                    //    switch (groupName.ToString())
                    //    {
                    //        case "MESSAGEOBJECT":
                    //        case "UNITNAME":
                    //        case "PROJECTNAME":
                    //        case "REPLICATION":
                    //            OtherGroupSectionGraph.Add(dbTypeGroup);
                    //            break;
                    //        default:
                    //            ObjectGroupSectionGraph.Add(dbTypeGroup);
                    //            break;
                    //    }
                    //}
                }
                else if (String.IsNullOrEmpty(dbObject.DbType))
                {
                    graph.ShapeType = ShapeTypes.Image;

                    XElement entryElement = graph.CreateNodeElement(dbObject);
                    if (entryElement != null)
                    {
                        //dbObject.Id = dbTypeId + "::n" + dbObject.Id;
                        dbObject.Path = dbTypeId + "::n" + dbObject.Id;
                        entryElement.SetAttributeValue("id", dbObject.Path);
                        DBGroupSectionGraph.Add(entryElement);
                        dbObjects[dbObject.Id] = dbObject;
                        objectList[i] = dbObject;
                        
                    }
                    graph.ShapeType = ShapeTypes.Figure;
                }
            }
            #endregion


            //#region groups
            //for (int i = 0; i < dbGroups.Count; i++)
            //{
            //    DBEntryDescriptor group = dbGroups[i];
            //    XElement entryElement = graph.CreateNodeElement(group);
            //    //XElement groupElement = graph.CreateGroupElement(group);
            //    if (entryElement != null)
            //    {
            //        /*if (group.Name.StartsWith("r", false, ci))
            //        {
            //            group.Id = "r0::n" + group.Id;
            //            entryElement.SetAttributeValue("id", group.Id);
            //            ReadOnlyGroupSectionGraph.Add(entryElement);
            //        }
            //        else if (group.Name.StartsWith("c", false, ci))
            //        {
            //            group.Id = "c0::n" + group.Id;
            //            entryElement.SetAttributeValue("id", group.Id);
            //            ChangeGroupSectionGraph.Add(entryElement);
            //        }
            //        else*/
            //        {
            //            group.Id = "o0::n" + group.Id;
            //            entryElement.SetAttributeValue("id", group.Id);
            //            OtherGroupSectionGraph.Add(entryElement);
            //        }

            //        dbGroups[i] = group;
            //    }
            //}
            //#endregion

            #region edges
            int counterId = 1;

            foreach (DBEntryDescriptor dbObject in objectList)
            {
                foreach (KeyValuePair<string,string> childEntryId in dbObject.Members)
                {
                    IEnumerable<XElement> similarEdges = 
                        from el in xmlRootSectionGraph.Descendants(xmlRootSectionGraph.Name.Namespace + "edge")
                        where (string)el.Attribute("source") == dbObject.Path 
                            && (string)el.Attribute("target") == dbObjects[childEntryId.Key].Path
                        select el;

                    if (similarEdges.Count()==0)
                    {
                        XElement edgeElement = graph.CreateEdgeElement("e" + counterId.ToString(), dbObject, dbObjects[childEntryId.Key], childEntryId.Value);
                        if (edgeElement != null)
                        {
                            xmlRootSectionGraph.Add(edgeElement);
                            counterId++;
                        }
                    }
                    else {
                        foreach (XElement edge in similarEdges)
                        {
                            edge.Element(xmlRootSectionGraph.Name.Namespace + "data")
                                .Descendants(graph.YedNS + "PolyLineEdge")
                                    .Descendants(graph.YedNS + "Arrows").FirstOrDefault()
                                        .SetAttributeValue("source", "standard");
                        }
                    }
                }

            }
            #endregion

            if (AppGroupHasElements == true || DBGroupHasElements == true)
                xmlRootSectionGraph.Add(AppGroup, DBGroup);

            graph.Root.Add(xmlRootSectionGraph);
            graph.Root.Save(YedOutputFilePath);
        }
    }

    public class NodeDescriptor
    {
        public string Name;
        public string Id;
        public string GroupId;
        public string FillColor = "#FFCC00";//#caecff84
        public string ActiveColor = "#FFCC00";
        public string NonActiveColor = "#CCFFFF";
        public string Width = "32";
        public string Height = "32";
        public string Transparent = "false";
        public string BorderType = "line";
        public string BorderWidth = "1";
        public string BorderColor = "#000000";
        public string LabelAlignment = "center";
        public string LabelFontFamily = "Dialog";
        public string LabelFontSize = "13";
        public string LabelTextColor = "#000000";
        public string LabelAutoSizePolicy = "content";
        public string Shape; //= "rectangle"
        public string Url;
        public string ImageSrc;
        public string ActiveImageName;
        public string NonActiveImageName;
        public bool IsImage = false;
        public string Parent;
        public string ModelName = "null";
        public string ModelPosition;
        public string Description;

        public NodeDescriptor()
        {
            BorderType = "line";
            BorderWidth = "1";
            BorderColor = "#000000";
            LabelAlignment = "center";
            LabelFontFamily = "Dialog";
            LabelFontSize = "13";
            LabelTextColor = "#000000";
            LabelAutoSizePolicy = "content";
            Shape = "rectangle";
            Url = "";
            ImageSrc = "";
            ModelName = "null";
            ModelPosition = "null";
            ActiveImageName = "";
            NonActiveImageName = "";
        }
    }

    public class GraphBuilder
    {
        #region properties
        public XElement Root;
        public XNamespace GraphNS = "http://graphml.graphdrawing.org/xmlns";
        public XNamespace XsiNS = "http://www.w3.org/2001/XMLSchema-instance";
        public XNamespace YedNS = "http://www.yworks.com/xml/graphml";
        public ShapeTypes ShapeType = ShapeTypes.Figure; // 1|2|3(Picture)
        public GraphTypes GraphType = GraphTypes.Undefined; // 1(All)|2(AllAndUser)|3(UserOnly) 

        private string imagePath = "file:/C:/Images/";

        #endregion

        public GraphBuilder() //Dictionary<string, DBEntryDescriptor> Groups
        {
            LoadGraph(); //Groups
        }

        private void SetColor(bool IsEnabled, ref NodeDescriptor GrNode)
        {
            switch (GraphType)
            {
                case GraphTypes.Undefined:
                case GraphTypes.All:
                    if (ShapeType == ShapeTypes.Picture)
                        GrNode.ImageSrc = imagePath + GrNode.ActiveImageName;
                    else
                        GrNode.FillColor = GrNode.ActiveColor;

                    break;

                case GraphTypes.AllAndUser:
                case GraphTypes.UserOnly:
                    if (ShapeType == ShapeTypes.Picture)
                        GrNode.ImageSrc = imagePath + (IsEnabled ? GrNode.ActiveImageName : GrNode.NonActiveImageName);
                    else
                        GrNode.FillColor = (IsEnabled ? GrNode.ActiveColor : GrNode.NonActiveColor);

                    break;
                default:
                    break;
            }
        }

        public void LoadGraph() //Dictionary<string, DBEntryDescriptor> Groups
        {
            //Document = new XmlDocument();
            //XmlProcessingInstruction newPI = Document.CreateProcessingInstruction("xml", "version='1.0' encoding='UTF-8' standalone='no'");
            //Document.AppendChild(newPI);
            //XmlElement xmlSection = Document.CreateElement("graphml");

            Root = new XElement(GraphNS + "graphml",
                new XAttribute(XNamespace.Xmlns + "xsi", XsiNS.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "y", YedNS.NamespaceName),
                new XAttribute(XsiNS + "schemaLocation", "http://graphml.graphdrawing.org/xmlns http://www.yworks.com/xml/schema/graphml/1.1/ygraphml.xsd"),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d0"), //R_ID
                    new XAttribute("for", "graphml"),
                    new XAttribute("yfiles.type", "resources")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d1"), //URL_ID
                    new XAttribute("for", "node"),
                    new XAttribute("attr.name", "url"),
                    new XAttribute("attr.type", "string")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d2"), //NODE_DESC_ID
                    new XAttribute("for", "node"),
                    new XAttribute("attr.name", "description"),
                    new XAttribute("attr.type", "string")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d3"), //N_ID
                    new XAttribute("for", "node"),
                    new XAttribute("yfiles.type", "nodegraphics")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d4"), //EDGE_URL_ID
                    new XAttribute("for", "edge"),
                    new XAttribute("attr.name", "description"),
                    new XAttribute("attr.type", "string")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d5"), //EDGE_DESC_ID
                    new XAttribute("for", "edge"),
                    new XAttribute("attr.name", "description"),
                    new XAttribute("attr.type", "string")
                ),
                new XElement(GraphNS + "key",
                    new XAttribute("id", "d6"), //EDGE_ID
                    new XAttribute("for", "edge"),
                    new XAttribute("yfiles.type", "edgegraphics")
                )

            );
        }

        private NodeDescriptor CreateNewNode(DBEntryDescriptor entry)
        {
            NodeDescriptor newNode = new NodeDescriptor();
            newNode.Name = entry.Name;
            newNode.Id = entry.Id;
            newNode.Description = (String.IsNullOrEmpty(entry.Description) ? entry.DbType : entry.Description) ;
            //if (!string.IsNullOrEmpty(entry.Url)) newNode.Url = @"javascript:void(window.open('" + entry.Url + "','_blank'))";

            switch (ShapeType)
            {
                case ShapeTypes.Figure:
                    newNode.Shape = "rectangle";
                    break;
                case ShapeTypes.Image:
                    newNode.Shape = entry.Shape;
                    if (String.IsNullOrEmpty(newNode.Shape))
                        newNode.Shape = "ellipse";
                    break;
                case ShapeTypes.Picture:
                    newNode.IsImage = true;
                    newNode.ActiveImageName = "user_on.jpg";
                    newNode.NonActiveImageName = "user_off.jpg";
                    break;
                default:
                    break;
            }
            SetColor(true, ref newNode);

            return newNode;
        }

        public XElement CreateGroupElement(DBEntryDescriptor group)
        {
            XElement xmlGroupNode = new XElement(GraphNS + "node",
                    new XAttribute("id", group.Id),
                    new XAttribute("yfiles.foldertype", "group")
            );

            if (group.Description != null)
            {
                XElement xmlGroupDescription = new XElement(GraphNS + "data",
                        new XAttribute("key", "d2"),
                        group.Description
                );
                xmlGroupNode.Add(xmlGroupDescription);
            }

            XElement xmlGroupData3 = new XElement(GraphNS + "data",
                    new XAttribute("key", "d3"), //N_ID
                    new XElement(YedNS + "ProxyAutoBoundsNode",
                        new XElement(YedNS + "Realizers",
                            new XAttribute("active", "0"),
                            new XElement(YedNS + "GroupNode",
                                new XElement(YedNS + "Fill",
                                    new XAttribute("color", "#CAECFF84"),
                                    new XAttribute("transparent", "false")
                                ),
                                new XElement(YedNS + "NodeLabel",
                                    new XAttribute("alignment", "left"),
                                    new XAttribute("modelName", "internal"),
                                    new XAttribute("modelPosition", "t"),
                                    new XAttribute("backgroundColor", "#99CCFF"),
                                    group.Name
                                ),
                                new XElement(YedNS + "State",
                                    new XAttribute("closed", "false")
                                )
                            ),
                            new XElement(YedNS + "GroupNode",
                                new XElement(YedNS + "Fill",
                                    new XAttribute("color", "#CAECFF84"),
                                    new XAttribute("transparent", "false")
                                ),
                                new XElement(YedNS + "NodeLabel",
                                    new XAttribute("alignment", "left"),
                                    new XAttribute("modelName", "internal"),
                                    new XAttribute("modelPosition", "t"),
                                    new XAttribute("backgroundColor", "#99CCFF"),
                                    (group.Name.Length <= 20 ? group.Name : group.Name.Substring(0, 20) + "...")
                                ),
                                new XElement(YedNS + "State",
                                    new XAttribute("closed", "true")
                                )
                            )
                        )
                    )
            );
            xmlGroupNode.Add(xmlGroupData3);

            XElement xmlGrap = new XElement(GraphNS + "graph",
                    new XAttribute("edgedefault", "directed"),
                    new XAttribute("id", group.Id + ":")
            );
            xmlGroupNode.Add(xmlGrap);

            return xmlGroupNode;
        }

        //new graphml node
        public XElement CreateNodeElement(DBEntryDescriptor entry)
        {
            XElement xmlNode = new XElement(GraphNS + "node",
                new XAttribute("id", "n" + entry.Id)
            );

            NodeDescriptor Node = CreateNewNode(entry); //.Name, entry.Id, entry.Description, 
            //Node.GroupId = XmlSection.GetAttribute("id") +":"+ Node.Id; 
            //XmlElement xmlNode = this.Document.CreateElement("node");
            //xmlNode.SetAttribute("id", Node.GroupId);
            if (!string.IsNullOrEmpty(entry.Url))
            {
                XElement xmlUrl = new XElement(GraphNS + "data",
                    new XAttribute("key", "d1"), //URL_ID
                    entry.Url
                );
                xmlNode.Add(xmlUrl);
            }
            if (entry.Description != null)
            {
                XElement xmlGroupDescription = new XElement(GraphNS + "data",
                    new XAttribute("key", "d2"), //NODE_DESC_ID
                    entry.Description
                );
                xmlNode.Add(xmlGroupDescription);

            }
            XElement xmlImageShape;
            if (Node.IsImage)
            {
                xmlImageShape = new XElement(YedNS + "Image",
                        new XAttribute("href", Node.ImageSrc)
                        );
            }
            else
            {
                xmlImageShape = new XElement(YedNS + "Shape",
                        new XAttribute("type", Node.Shape)
                        );
            }

            XElement xmlGroupData3 = new XElement(GraphNS + "data",
                new XAttribute("key", "d3"), //N_ID
                new XElement(YedNS + (Node.IsImage ? "ImageNode" : "ShapeNode"),
                    new XElement(YedNS + "Geometry",
                        new XAttribute("width", Node.Width),
                        new XAttribute("height", Node.Height)
                    ),
                    new XElement(YedNS + "Fill",
                        new XAttribute("color", Node.FillColor),
                        new XAttribute("transparent", Node.Transparent)
                    ),
                    new XElement(YedNS + "BorderStyle",
                        new XAttribute("type", Node.BorderType),
                        new XAttribute("width", Node.BorderWidth),
                        new XAttribute("color", Node.BorderColor)
                    ),
                    new XElement(YedNS + "NodeLabel",
                        new XAttribute("alignment", Node.LabelAlignment),
                        new XAttribute("fontFamily", Node.LabelFontFamily),
                        new XAttribute("textColor", Node.LabelTextColor),
                        new XAttribute("autoSizePolicy", Node.LabelAutoSizePolicy),
                        new XAttribute("modelName", Node.ModelName),
                        new XAttribute("modelPosition", Node.ModelPosition),
                        Node.Name
                    ),
                    xmlImageShape
                )
            );
            xmlNode.Add(xmlGroupData3);
            //xmlNode.Add(new XElement(GraphNS + "data",
            //            new XAttribute("key", "d1"), //URL_ID
            //            Node.Url
            //            )
            //    );

            return xmlNode;
        }
        //<y:EdgeLabel alignment="center" distance="2.0" 
        //  fontFamily="Dialog" fontSize="12" fontStyle="plain" hasBackgroundColor="false" 
        //  hasLineColor="false" modelName="six_pos" modelPosition="tail" 
        //  preferredPlacement="anywhere" ratio="0.5" 
        //  textColor="#000000" visible="true" width="75.384765625">select,update
        //</y:EdgeLabel>
        public XElement CreateEdgeElement(string Id, DBEntryDescriptor parentNode, DBEntryDescriptor childNode, string edgeLabel)
        {
            XElement xmlEdge = new XElement(GraphNS + "edge",
                new XAttribute("id", Id), //childNode.Id + "_" + parentNode.Id
                new XAttribute("source", childNode.Path), //Id
                new XAttribute("target", parentNode.Path), //Id
                new XElement(GraphNS + "data",
                    new XAttribute("key", "d6"), //EDGE_ID
                    new XElement(YedNS + "PolyLineEdge",
                        new XElement(YedNS + "LineStyle",
                            new XAttribute("type", "dashed"),
                            new XAttribute("width", "1.0"),
                            new XAttribute("color", "#000000")
                        ),
                        new XElement(YedNS + "Arrows",
                            new XAttribute("source", "none"),
                            new XAttribute("target", "standard")
                        ),
                        new XElement(YedNS + "EdgeLabel",
                            new XAttribute("fontSize", "9"),
                            new XAttribute("fontStyle", "plain"),
                            new XAttribute("width", "1.0"),
                            new XAttribute("alignment", "center"),
                            edgeLabel
                        ),
                        new XElement(YedNS + "BendStyle",
                            new XAttribute("smoothed", "false")
                        )
                    )
                )
            );
            return xmlEdge;
        }

    }
}
