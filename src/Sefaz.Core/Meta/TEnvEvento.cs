namespace Sefaz.Core.Meta
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.portalfiscal.inf.br/nfe")]
    [System.Xml.Serialization.XmlRootAttribute("envEvento", Namespace = "http://www.portalfiscal.inf.br/nfe", IsNullable = false)]
    public partial class TEnvEvento
    {

        private string idLoteField;

        private TEvento[] eventoField;

        private string versaoField;

        /// <remarks/>
        public string idLote
        {
            get
            {
                return this.idLoteField;
            }
            set
            {
                this.idLoteField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("evento")]
        public TEvento[] evento
        {
            get
            {
                return this.eventoField;
            }
            set
            {
                this.eventoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string versao
        {
            get
            {
                return this.versaoField;
            }
            set
            {
                this.versaoField = value;
            }
        }
    }
}
