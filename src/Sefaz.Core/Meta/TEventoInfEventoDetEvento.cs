namespace Sefaz.Core.Meta
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.portalfiscal.inf.br/nfe")]
    public partial class TEventoInfEventoDetEvento
    {

        private TEventoInfEventoDetEventoDescEvento descEventoField;

        private string xJustField;

        private TEventoInfEventoDetEventoVersao versaoField;

        /// <remarks/>
        public TEventoInfEventoDetEventoDescEvento descEvento
        {
            get
            {
                return this.descEventoField;
            }
            set
            {
                this.descEventoField = value;
            }
        }

        /// <remarks/>
        public string xJust
        {
            get
            {
                return this.xJustField;
            }
            set
            {
                this.xJustField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TEventoInfEventoDetEventoVersao versao
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
