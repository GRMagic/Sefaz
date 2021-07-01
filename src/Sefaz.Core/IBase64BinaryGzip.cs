namespace Sefaz.Core
{
    /// <summary>
    /// Objeto contém valor compactado com gzip em base64
    /// </summary>
    public interface IBase64BinaryGzip
    {
        /// <remarks/>
        byte[] Value { get; }
    }
}
