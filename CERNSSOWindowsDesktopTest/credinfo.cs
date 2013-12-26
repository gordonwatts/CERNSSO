using System;
using System.Runtime.InteropServices;

namespace CERNSSOWindowsDesktopTest
{
    class NativeMethods
    {
        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag,
                          [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CredentialInMarshaler))]out Credential credential);
    }

    public enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,      // Maximum supported cred type
        MAXIMUM_EX = (MAXIMUM + 1000),  // Allow new applications to run on old OSes
    }

    public enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct CREDENTIAL_ATTRIBUTE
    {
        string Keyword;
        uint Flags;
        uint ValueSize;
        IntPtr Value;
    }

    //This type is deliberately not designed to be marshalled.
    public class Credential
    {
        public UInt32 Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public byte[] CredentialBlob;
        public CRED_PERSIST Persist;
        public CREDENTIAL_ATTRIBUTE[] Attributes;
        public string TargetAlias;
        public string UserName;
    }

    /// <summary>
    /// 
    /// </summary>
    public class CredentialInMarshaler : ICustomMarshaler
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class NATIVECREDENTIAL
        {
            public UInt32 Flags;
            public CRED_TYPE Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public UInt32 CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CRED_PERSIST Persist;
            public UInt32 AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        public void CleanUpManagedData(object ManagedObj)
        {
            // Nothing to do since all data can be garbage collected.
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return;
            }
        }

        public int GetNativeDataSize()
        {
            throw new NotImplementedException();
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            throw new NotImplementedException();
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return null;
            }

            NATIVECREDENTIAL lRawCredential = (NATIVECREDENTIAL)Marshal.PtrToStructure(pNativeData, typeof(NATIVECREDENTIAL));

            Credential lCredential = new Credential()
            {
                UserName = lRawCredential.UserName,
                TargetName = lRawCredential.TargetName,
                TargetAlias = lRawCredential.TargetAlias,
                Persist = lRawCredential.Persist,
                Comment = lRawCredential.Comment,
                Flags = lRawCredential.Flags,
                LastWritten = lRawCredential.LastWritten,
                Type = lRawCredential.Type,
                CredentialBlob = new byte[lRawCredential.CredentialBlobSize],
                Attributes = new CREDENTIAL_ATTRIBUTE[lRawCredential.AttributeCount]
            };

            Marshal.Copy(lRawCredential.CredentialBlob, lCredential.CredentialBlob, 0, (int)lRawCredential.CredentialBlobSize);

            return lCredential;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return new CredentialInMarshaler();
        }
    }
}
