using YamlDotNet.Serialization;

namespace UnityXRefMap.Yaml
{
    internal class XRefMapReference
    {
        [YamlMember(Alias = "uid")] public string Uid;
        [YamlMember(Alias = "name")] public string Name;
        [YamlMember(Alias = "href")] public string Href;
        [YamlMember(Alias = "commentId")] public string CommentId;
        [YamlMember(Alias = "fullName")] public string FullName;
        [YamlMember(Alias = "nameWithType")] public string NameWithType;
    }
}
