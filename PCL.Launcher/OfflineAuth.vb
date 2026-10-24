Imports System
Imports System.Security.Cryptography
Imports System.Text

Public Module OfflineAuth

    ''' <summary>
    ''' 根据玩家名生成 Minecraft 离线模式使用的 UUID。
    ''' </summary>
    Public Function GetOfflineUuid(playerName As String) As String
        Dim raw = "OfflinePlayer:" & playerName
        Dim bytes = Encoding.UTF8.GetBytes(raw)

        Using md5 As MD5 = MD5.Create()
            Dim hash = md5.ComputeHash(bytes)

            ' 按照 RFC 4122 标准设置 version (3) 和 variant
            hash(6) = (hash(6) And &H0F) Or &H30
            hash(8) = (hash(8) And &H3F) Or &H80

            Return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
        End Using
    End Function

End Module
