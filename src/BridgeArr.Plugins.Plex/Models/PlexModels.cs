using System.Xml.Serialization;

namespace BridgeArr.Plugins.Plex.Models;

[XmlRoot("MediaContainer")]
public class PlexMediaContainer
{
    [XmlAttribute("size")]
    public int Size { get; set; }

    [XmlElement("Video")]
    public List<PlexVideo> Videos { get; set; } = new();

    [XmlElement("Directory")]
    public List<PlexDirectory> Directories { get; set; } = new();
}

public class PlexVideo
{
    [XmlAttribute("ratingKey")]
    public string RatingKey { get; set; } = string.Empty;

    [XmlAttribute("title")]
    public string Title { get; set; } = string.Empty;

    [XmlAttribute("year")]
    public int Year { get; set; }

    [XmlElement("Label")]
    public List<PlexLabel> Labels { get; set; } = new();
}

public class PlexDirectory
{
    [XmlAttribute("ratingKey")]
    public string RatingKey { get; set; } = string.Empty;

    [XmlAttribute("title")]
    public string Title { get; set; } = string.Empty;

    [XmlAttribute("year")]
    public int Year { get; set; }

    [XmlElement("Label")]
    public List<PlexLabel> Labels { get; set; } = new();
}

public class PlexLabel
{
    [XmlAttribute("tag")]
    public string Tag { get; set; } = string.Empty;

    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;
}

public class PlexLibrarySection
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
