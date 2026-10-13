using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;

namespace SistemaGestionHumanaGR
{
    public class cls_codificacion
    {
        public static object CargarArchivoXmlEncriptado(string ruta_archivo, Type tipo, ref string msj)
        {
            StreamReader sr = cls_codificacion.DecryptFile(ruta_archivo, ref msj);
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
                    if (cls_codificacion.EncryptFile(archivo, ref msj) == true)
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

        private static StreamReader DecryptFile(string archivo, ref string msj)
        {
            string sKey = System.Configuration.ConfigurationManager.AppSettings["encryptor"].ToString();
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
                DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                DES.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                DES.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                FileStream fsread = new FileStream(archivo, FileMode.Open, FileAccess.Read);
                ICryptoTransform desdecrypt = DES.CreateDecryptor();
                CryptoStream cryptostreamDecr = new CryptoStream(fsread, desdecrypt, CryptoStreamMode.Read);
                StreamReader str = new StreamReader(cryptostreamDecr);
                gch.Free();
                return str;
            }
            catch (Exception ex)
            {
                msj = ex.Message;
                return null;
            }
        }

        private static bool EncryptFile(string archivo, ref string msj)
        {
            string sKey = System.Configuration.ConfigurationManager.AppSettings["encryptor"].ToString();
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
                DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                DES.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                DES.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                ICryptoTransform desencrypt = DES.CreateEncryptor();
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
            catch (Exception ex)
            {
                msj = ex.Message;
                return false;
            }
        }
    }
}
