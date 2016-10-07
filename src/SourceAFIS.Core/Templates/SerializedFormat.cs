using System;
using System.IO;

namespace SourceAFIS.Templates
{
    public sealed class SerializedFormat : TemplateFormatBase<Template>
    {
        public override Template Export(TemplateBuilder builder)
        {
            return new Template(builder);
        }

        public override TemplateBuilder Import(Template template)
        {
            return template.ToTemplateBuilder();
        }

        public override void Serialize(Stream stream, Template template)
        {
            //TODO: Fix
            throw new NotImplementedException();
            //var formatter = new BinaryFormatter();
            //formatter.Serialize(stream, template);
        }

        public override Template Deserialize(Stream stream)
        {
            //TODO: Fix
            throw new NotImplementedException();
            //var formatter = new BinaryFormatter();
            //return formatter.Deserialize(stream) as Template;
        }
    }
}
