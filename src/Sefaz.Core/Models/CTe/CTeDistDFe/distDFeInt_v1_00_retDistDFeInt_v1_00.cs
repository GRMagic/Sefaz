﻿//------------------------------------------------------------------------------
// <auto-generated>
//     O código foi gerado por uma ferramenta.
//     Versão de Tempo de Execução:4.0.30319.42000
//
//     As alterações ao arquivo poderão causar comportamento incorreto e serão perdidas se
//     o código for gerado novamente.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 
namespace Sefaz.Core.Models.CTe {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.portalfiscal.inf.br/cte", IsNullable=false)]
    public partial class distDFeInt {
        
        private TAmb tpAmbField;
        
        private TCodUfIBGE cUFAutorField;
        
        private string itemField;
        
        private TipoPessoa itemElementNameField;
        
        private object item1Field;
        
        private TVerDistDFe versaoField;
        
        /// <remarks/>
        public TAmb tpAmb {
            get {
                return this.tpAmbField;
            }
            set {
                this.tpAmbField = value;
            }
        }
        
        /// <remarks/>
        public TCodUfIBGE cUFAutor {
            get {
                return this.cUFAutorField;
            }
            set {
                this.cUFAutorField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("CNPJ", typeof(string))]
        [System.Xml.Serialization.XmlElementAttribute("CPF", typeof(string))]
        [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
        public string CpjCnpj {
            get {
                return this.itemField;
            }
            set {
                this.itemField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public TipoPessoa ItemElementName {
            get {
                return this.itemElementNameField;
            }
            set {
                this.itemElementNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("consNSU", typeof(distDFeIntConsNSU))]
        [System.Xml.Serialization.XmlElementAttribute("distNSU", typeof(distDFeIntDistNSU))]
        public object Item1 {
            get {
                return this.item1Field;
            }
            set {
                this.item1Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TVerDistDFe versao {
            get {
                return this.versaoField;
            }
            set {
                this.versaoField = value;
            }
        }
    }
        
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    public partial class distDFeIntConsNSU {
        
        private string nSUField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="token")]
        public string NSU {
            get {
                return this.nSUField;
            }
            set {
                this.nSUField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    public partial class distDFeIntDistNSU {
        
        private string ultNSUField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="token")]
        public string ultNSU {
            get {
                return this.ultNSUField;
            }
            set {
                this.ultNSUField = value;
            }
        }
    }
  
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.portalfiscal.inf.br/cte", IsNullable=false)]
    public partial class retDistDFeInt {
        
        private TAmb tpAmbField;
        
        private string verAplicField;
        
        private string cStatField;
        
        private string xMotivoField;
        
        private string dhRespField;
        
        private string ultNSUField;
        
        private string maxNSUField;
        
        private retDistDFeIntLoteDistDFeInt loteDistDFeIntField;
        
        private TVerDistDFe versaoField;
        
        /// <remarks/>
        public TAmb tpAmb {
            get {
                return this.tpAmbField;
            }
            set {
                this.tpAmbField = value;
            }
        }
        
        /// <remarks/>
        public string verAplic {
            get {
                return this.verAplicField;
            }
            set {
                this.verAplicField = value;
            }
        }
        
        /// <remarks/>
        public string cStat {
            get {
                return this.cStatField;
            }
            set {
                this.cStatField = value;
            }
        }
        
        /// <remarks/>
        public string xMotivo {
            get {
                return this.xMotivoField;
            }
            set {
                this.xMotivoField = value;
            }
        }
        
        /// <remarks/>
        public string dhResp {
            get {
                return this.dhRespField;
            }
            set {
                this.dhRespField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="token")]
        public string ultNSU {
            get {
                return this.ultNSUField;
            }
            set {
                this.ultNSUField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="token")]
        public string maxNSU {
            get {
                return this.maxNSUField;
            }
            set {
                this.maxNSUField = value;
            }
        }
        
        /// <remarks/>
        public retDistDFeIntLoteDistDFeInt loteDistDFeInt {
            get {
                return this.loteDistDFeIntField;
            }
            set {
                this.loteDistDFeIntField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TVerDistDFe versao {
            get {
                return this.versaoField;
            }
            set {
                this.versaoField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    public partial class retDistDFeIntLoteDistDFeInt {
        
        private retDistDFeIntLoteDistDFeIntDocZip[] docZipField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("docZip")]
        public retDistDFeIntLoteDistDFeIntDocZip[] docZip {
            get {
                return this.docZipField;
            }
            set {
                this.docZipField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.portalfiscal.inf.br/cte")]
    public partial class retDistDFeIntLoteDistDFeIntDocZip : IBase64BinaryGzip{
        
        private string nSUField;
        
        private string schemaField;
        
        private byte[] valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="token")]
        public string NSU {
            get {
                return this.nSUField;
            }
            set {
                this.nSUField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string schema {
            get {
                return this.schemaField;
            }
            set {
                this.schemaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType="base64Binary")]
        public byte[] Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
}
