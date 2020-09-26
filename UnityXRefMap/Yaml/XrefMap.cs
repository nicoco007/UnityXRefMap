using YamlDotNet.Serialization;

namespace UnityXRefMap.Yaml
{
    internal class XRefMap
    {
        [YamlMember(Alias = "sorted")] public bool Sorted;
        [YamlMember(Alias = "references")] public XRefMapReference[] References;
    }
}
