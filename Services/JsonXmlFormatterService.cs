using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace SwissKnifeApp.Services
{
    public class JsonXmlFormatterService
    {
        public bool IsJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.TrimStart();
            return t.StartsWith("{") || t.StartsWith("[");
        }

        public bool IsXml(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.TrimStart().StartsWith("<");
        }

        public string BeautifyJson(string json)
        {
            var token = JToken.Parse(json);
            return token.ToString(Formatting.Indented);
        }

        public string MinifyJson(string json)
        {
            var token = JToken.Parse(json);
            return token.ToString(Formatting.None);
        }

        public string BeautifyXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            using var sw = new StringWriter();
            using var xw = new XmlTextWriter(sw) { Formatting = System.Xml.Formatting.Indented };
            doc.WriteTo(xw);
            return sw.ToString();
        }

        public string MinifyXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.OuterXml;
        }

        public string JsonToXml(string json, string rootName = "Root")
        {
            var doc = JsonConvert.DeserializeXmlNode(json, rootName);
            if (doc == null)
                throw new InvalidOperationException("JSON XML'e dönüştürülemedi (DeserializeXmlNode null döndü).");
            return BeautifyXml(doc.OuterXml);
        }

        public string XmlToJson(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return JsonConvert.SerializeXmlNode(doc, Formatting.Indented, true);
        }

        public bool TryValidateJson(string json, out string? error)
        {
            try { JToken.Parse(json); error = null; return true; }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        public bool TryValidateXml(string xml, out string? error)
        {
            try { var doc = new XmlDocument(); doc.LoadXml(xml); error = null; return true; }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        public string? JsonQuery(string json, string jsonPath)
        {
            var token = JToken.Parse(json).SelectToken(jsonPath);
            return token?.ToString();
        }

        public string? XmlQuery(string xml, string xPath)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var node = doc.SelectSingleNode(xPath);
            return node?.OuterXml;
        }
    }
}
