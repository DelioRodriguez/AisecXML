using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NominaPrueba
{
    public class ClsCodificacion
    {
        public static object? CargarArchivoXmlEncriptado(string ruta_archivo, Type tipo, ref string msj)
        {
            StreamReader? sr = ClsCodificacion.DecryptFile(ruta_archivo, ref msj);
            if (sr != null)
            {
                try
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(tipo);
                    return xs.Deserialize(sr);
                }
                catch (Exception ex)
                {
                    msj = ex.Message;
                }
            }
            return null;
        }

        public static bool GuardarArchivoXmlEncriptado(string archivo, object objeto, Type tipo, ref string msj)
        {
            using (System.IO.TextWriter tw = new System.IO.StreamWriter(archivo))
            {
                try
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(tipo);
                    xs.Serialize(tw, objeto);
                    tw.Close();
                    if (ClsCodificacion.EncryptFile(archivo, ref msj) == true)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    msj = ex.Message;
                    return false;
                }
            }
        }

        private static StreamReader? DecryptFile(string archivo, ref string msj)
        {
            string? sKey = System.Configuration.ConfigurationManager.AppSettings["encryptor"];
            if (sKey == null)
            {
                msj = "Key not found";
                return null;
            }
            
            if (sKey.Length < 8)
            {
                sKey = sKey.PadLeft(8, '0');
            }
            else if (sKey.Length > 8)
            {
                sKey = sKey.Substring(0, 8);
            }
            try
            {
                GCHandle gch = GCHandle.Alloc(sKey, GCHandleType.Pinned);
                using (DES des = DES.Create())
                {
                    des.Key = Encoding.ASCII.GetBytes(sKey);
                    des.IV = Encoding.ASCII.GetBytes(sKey);
                    FileStream fsread = new FileStream(archivo, FileMode.Open, FileAccess.Read);
                    ICryptoTransform desdecrypt = des.CreateDecryptor();
                    CryptoStream cryptostreamDecr = new CryptoStream(fsread, desdecrypt, CryptoStreamMode.Read);
                    StreamReader str = new StreamReader(cryptostreamDecr);
                    gch.Free();
                    return str;
                }
            }
            catch (Exception ex)
            {
                msj = ex.Message;
                return null;
            }
        }

        private static bool EncryptFile(string archivo, ref string msj)
        {
            string? sKey = System.Configuration.ConfigurationManager.AppSettings["encryptor"];
            if (sKey == null)
            {
                msj = "Key not found";
                return false;
            }

            if (sKey.Length < 8)
            {
                sKey = sKey.PadLeft(8, '0');
            }
            else if (sKey.Length > 8)
            {
                sKey = sKey.Substring(0, 8);
            }
            try
            {
                GCHandle gch = GCHandle.Alloc(sKey, GCHandleType.Pinned);
                FileStream fsInput = new FileStream(archivo, FileMode.Open, FileAccess.Read);
                using (DES des = DES.Create())
                {
                    des.Key = Encoding.ASCII.GetBytes(sKey);
                    des.IV = Encoding.ASCII.GetBytes(sKey);
                    ICryptoTransform desencrypt = des.CreateEncryptor();
                    byte[] bytearrayinput = new byte[fsInput.Length];
                    fsInput.Read(bytearrayinput, 0, bytearrayinput.Length);
                    fsInput.Close();
                    FileStream fsEncrypted = new FileStream(archivo, FileMode.Create, FileAccess.Write);
                    CryptoStream cryptostream = new CryptoStream(fsEncrypted, desencrypt, CryptoStreamMode.Write);
                    cryptostream.Write(bytearrayinput, 0, bytearrayinput.Length);
                    cryptostream.Close();
                    fsEncrypted.Close();
                    gch.Free();
                    return true;
                }
            }
            catch (Exception ex)
            {
                msj = ex.Message;
                return false;
            }
        }
    }
}
