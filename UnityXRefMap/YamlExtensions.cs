using YamlDotNet.RepresentationModel;

namespace UnityXRefMap
{
    internal static class YamlExtensions
    {
        public static YamlMappingNode GetMappingNode(this YamlMappingNode node, YamlNode key)
        {
            if (node.Children.TryGetValue(key, out YamlNode tempNode))
            {
                return tempNode as YamlMappingNode;
            }
            else
            {
                return null;
            }
        }

        public static string GetScalarValue(this YamlMappingNode node, YamlNode key)
        {
            if (node.Children.TryGetValue(key, out YamlNode tempNode))
            {
                return (tempNode as YamlScalarNode)?.Value;
            }
            else
            {
                return null;
            }
        }
    }
}
