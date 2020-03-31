using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace WifiLedController {
    class XmlSettings {
        XmlDocument settings;
        readonly string FileUrl = "./Settings.xml";//settings file in same folder as working directory
        public XmlSettings() {
            //settings being null is a problem for loading
            settings = new XmlDocument();
        }

        public string FindCustomName(string macAddress) {
            XmlNode result = settings.SelectSingleNode("//CustomName[MacAddress=\""+ macAddress +"\"]/Name");
            Debug.WriteLine("[XmlSettings.FindCustomName({0})] result = {1}",macAddress,result);
            if(result == null) {
                return null;
            }
            return result.InnerText;
        }
        //Does adding and updating of elements
        public void AddCustomName(string macAddress, string name) {
            //Check if we have a node already
            XmlNode result = settings.DocumentElement.SelectSingleNode("//CustomName[MacAddress=\"" + macAddress + "\"]/Name");
            if (result == null) {//no existing node available
                //create CustomName element
                XmlElement customName = settings.CreateElement("CustomName");
                //Add macaddress to the custom name 
                XmlElement macAddressElement = settings.CreateElement("MacAddress");
                macAddressElement.InnerText = macAddress;
                customName.AppendChild(macAddressElement);
                //add name
                XmlElement nameElement = settings.CreateElement("Name");
                nameElement.InnerText = name;
                customName.AppendChild(nameElement);

                settings.DocumentElement.AppendChild(customName);
            } else {
                //simple update
                result.InnerText = name;
            }
        }
        //  X, Xwidth,Xstride, Y, Yheight, Ystride 
        public (int, int, int, int, int, int) ReadAmbianceSettings() {
            XmlNode result = settings.DocumentElement.SelectSingleNode("//AmbianceSettings");
            
            if(result != null) {//<X>0</X><Xwidth>1920</Xwidth><Xstride>10</Xstride><Y>1060</Y><Yheight>20</Yheight><Ystride>2</Ystride>
                Debug.WriteLine("[ReadAmbianceSettings()] " + result.InnerXml);
                //Parse the Xml 
                int X = int.Parse(result["X"].InnerText);
                int Xwidth = int.Parse(result["Xwidth"].InnerText);
                int Xstride = int.Parse(result["Xstride"].InnerText);
                int Y = int.Parse(result["Y"].InnerText);
                int Yheight = int.Parse(result["Yheight"].InnerText);
                int Ystride = int.Parse(result["Ystride"].InnerText);
                return (X,Xwidth,Xstride,Y,Yheight,Ystride);
            }
            //Default values
            return (0, 1920, 10, 1060, 20, 2);
        }

        public void AddAmbianceSettings(int X, int Xwidth, int Xstride, int Y, int Yheight, int Ystride) {

            XmlNode result = settings.DocumentElement.SelectSingleNode("//AmbianceSettings");
            
            Debug.WriteLine("[AddAmbianceSettings] {0}, {1}, {2}, {3}, {4}, {5}",X,Xwidth,Xstride,Y,Yheight,Ystride);
            if(result == null) {
                
                XmlElement colorSettings = settings.CreateElement("AmbianceSettings");

                //Set up the internal structure and values
                XmlElement setting = settings.CreateElement("X");
                setting.InnerText = X.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Xwidth");
                setting.InnerText = Xwidth.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Xstride");
                setting.InnerText = Xstride.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Y");
                setting.InnerText = Y.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Yheight");
                setting.InnerText = Yheight.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Ystride");
                setting.InnerText = Ystride.ToString();
                colorSettings.AppendChild(setting);

                settings.DocumentElement.AppendChild(colorSettings);

            } else {
                Debug.WriteLine("[AddAmbianceSettings] " + result.InnerXml);
                result["X"].InnerText = X.ToString();
                result["Xwidth"].InnerText = Xwidth.ToString();
                result["Xstride"].InnerText = Xstride.ToString();
                result["Y"].InnerText = Y.ToString();
                result["Yheight"].InnerText = Yheight.ToString();
                result["Ystride"].InnerText = Ystride.ToString();
            }
        }

        public (float, float, float, string) ReadAmbianceColorTuningSettings() {
            XmlNode result = settings.DocumentElement.SelectSingleNode("//AmbianceColorTuningSettings");
            
            if (result != null) {
                Debug.WriteLine("[ReadAmbianceColorTuningSettings()] " + result.InnerXml);
                //Parse the Xml 
                float red = float.Parse(result["Red"].InnerText);
                float green = float.Parse(result["Green"].InnerText);
                float blue = float.Parse(result["Blue"].InnerText);
                string mode = result["Mode"].InnerText;
                return (red,green,blue,mode);
            }
            //Default values
            return (0, 0, 0, "Additive");
        }

        public void AddAmbianceColorTuningSettings(float red, float green, float blue, string mode) {

            XmlNode result = settings.DocumentElement.SelectSingleNode("//AmbianceColorTuningSettings");
            //Debug.WriteLine("[AddAmbianceSettings] " + result.InnerXml);
            
            if (result == null) {
                XmlElement colorSettings = settings.CreateElement("AmbianceColorTuningSettings");

                //Set up the internal structure and values
                XmlElement setting = settings.CreateElement("Red");
                setting.InnerText = red.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Green");
                setting.InnerText = green.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Blue");
                setting.InnerText = blue.ToString();
                colorSettings.AppendChild(setting);

                setting = settings.CreateElement("Mode");
                setting.InnerText = mode;
                colorSettings.AppendChild(setting);

                settings.DocumentElement.AppendChild(colorSettings);

            } else {

                result["Red"].InnerText = red.ToString();
                result["Green"].InnerText = green.ToString();
                result["Blue"].InnerText = blue.ToString();
                result["Mode"].InnerText = mode;
            }
        }

        private string GenerateXMLSchemaText() {
            string schema =   "<?xml version = \"1.0\" encoding = \"utf-8\"?>"
                            + "<xs:schema targetNamespace = \"http://tempuri.org/XMLSchema.xsd\" "
                            + "elementFormDefault = \"qualified\" "
                            + "xmlns = \"http://tempuri.org/XMLSchema.xsd\" "
                            + "xmlns:mstns = \"http://tempuri.org/XMLSchema.xsd\" "
                            + "xmlns:xs = \"http://www.w3.org/2001/XMLSchema\">"
                            + "<xs:element name = \"Settings\" />"
                            + "<xs:element name = \"CustomName\" type = \"MacAddressNamePair\"/>"
                            + "<xs:complexType name = \"MacAddressNamePair\">"
                            + "<xs:sequence>"
                            + "<xs:element name = \"MacAddress\" type = \"xs:string\"/>"
                            + "<xs:element name = \"Name\" type = \"xs:string\"/>"
                            + "</xs:sequence>"
                            + "</xs:complexType>"
                            + "<xs:element name = \"AmbianceSettings\" type = \"AmbianceSettingsValues\"/>"
                            + "<xs:complexType name = \"AmbianceSettingsValues\">"
                            + "<xs:sequence>"     
                            + "<xs:element name = \"X\" type = \"xs:int\"/>"
                            + "<xs:element name = \"Xwidth\" type = \"xs:int\"/>"
                            + "<xs:element name = \"Xstride\" type = \"xs:int\"/>"
                            + "<xs:element name = \"Y\" type = \"xs:int\"/>"
                            + "<xs:element name = \"Yheight\" type = \"xs:int\"/>"
                            + "<xs:element name = \"Ystride\" type = \"xs:int\"/>"                       
                            + "</xs:sequence>"
                            + "</xs:complexType >"
                            + "<xs:element name=\"AmbianceColorTuningSettings\" type=\"AmbianceColorTuningSettingsValues\"/>"
                            + "<xs:complexType name=\"AmbianceColorTuningSettingsValues\">"
                            + "<xs:sequence>"
                            + "<xs:element name=\"Red\" type=\"xs:float\"/>"
                            + "<xs:element name=\"Green\" type=\"xs:float\"/>"
                            + "<xs:element name=\"Blue\" type=\"xs:float\"/>"
                            + "<xs:element name=\"Mode\" type=\"xs:string\"/>"
                            + "</xs:sequence>"
                            + "</xs:complexType>"
                            + "</xs:schema>";
            return schema;
        }

        private XmlSchema GetXmlSchema() {
            XmlSchemaSet xs = new XmlSchemaSet();
            XmlSchema schema;
            byte[] byteArray = Encoding.UTF8.GetBytes(GenerateXMLSchemaText());
            MemoryStream stream = new MemoryStream(byteArray);
            XmlReader reader = XmlReader.Create(stream);
            
            schema = xs.Add("http://tempuri.org/XMLSchema.xsd", reader);

            return schema;
        }
        //Try to load existing settings
        public void Load() {
            if (File.Exists(FileUrl)) {
                Debug.WriteLine("[XmlSettings.Load()] Attempting to load File.");
                try {//Try to load settings from file
                    //Debug.WriteLine(File.ReadAllText(FileUrl));
                    settings.Load(FileUrl);
                    Debug.WriteLine("[XmlSettings.Load()] File loaded..");
                    Debug.WriteLine(settings.InnerText);
                } catch (Exception e) {
                    Debug.WriteLine("[XmlSettings.Load()] File exists but Exception {0}",e);
                    Debug.WriteLine(e.StackTrace);
                    //Create an empty representations
                    //settings = new XmlDocument();
                    settings.AppendChild(settings.CreateXmlDeclaration("1.0", "utf-8", "yes"));
                    settings.AppendChild(settings.CreateElement("Settings"));//rootnode is settings
                }
            } else {
                //no existing file so create an empty one
                //settings = new XmlDocument();
                settings.AppendChild(settings.CreateXmlDeclaration("1.0", "utf-8", "yes"));
                settings.AppendChild(settings.CreateElement("Settings"));


            }
            settings.Schemas.Add(GetXmlSchema());

            
        }
    //************************************************************************************
    //
    //  Event handler that is raised when XML doesn't validate against the schema.
    //
    //************************************************************************************
    void settings_ValidationEventHandler(object sender,System.Xml.Schema.ValidationEventArgs e) {
        if (e.Severity == XmlSeverityType.Warning) {
            System.Windows.Forms.MessageBox.Show("The following validation warning occurred: " + e.Message);
        } else if (e.Severity == XmlSeverityType.Error) {
            System.Windows.Forms.MessageBox.Show("The following critical validation errors occurred: " + e.Message);
            Type objectType = sender.GetType();
        }
    }
    public void Save() {
            settings.Validate(settings_ValidationEventHandler);
            settings.Save(FileUrl);
        }



    }
}
