Imports System
Imports System.Net.Http
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Public Module MicrosoftAuth

    ' 第三方启动器通用的 Azure Client ID
    Private Const ClientId As String = "00000000402b5328"
    Private Const Scope As String = "XboxLive.signin offline_access"

    Public Class DeviceCodeResult
        Public Property UserCode As String
        Public Property DeviceCode As String
        Public Property VerificationUri As String
        Public Property ExpiresIn As Integer
        Public Property Interval As Integer
        Public Property Message As String
    End Class

    Public Class LoginResult
        Public Property Success As Boolean
        Public Property AccessToken As String
        Public Property Uuid As String
        Public Property PlayerName As String
        Public Property Message As String
    End Class

    ''' <summary>
    ''' 1. 发起设备代码流，获取 user_code 和 verification_uri。
    ''' </summary>
    Public Function RequestDeviceCode() As DeviceCodeResult
        Using client As New HttpClient()
            Dim content As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                {"client_id", ClientId},
                {"scope", Scope}
            })
            Dim resp = client.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", content).Result
            Dim json = JObject.Parse(resp.Content.ReadAsStringAsync().Result)

            If json("error") IsNot Nothing Then
                Throw New Exception("请求设备代码失败: " & json("error_description")?.ToString())
            End If

            Return New DeviceCodeResult With {
                .UserCode = json("user_code")?.ToString(),
                .DeviceCode = json("device_code")?.ToString(),
                .VerificationUri = json("verification_uri")?.ToString(),
                .ExpiresIn = json("expires_in").Value(Of Integer)(),
                .Interval = json("interval").Value(Of Integer)(),
                .Message = json("message")?.ToString()
            }
        End Using
    End Function

    ''' <summary>
    ''' 2. 轮询 Token 接口，等待用户完成授权。
    ''' </summary>
    Public Function PollForToken(deviceCode As String, interval As Integer, expiresIn As Integer) As String
        Using client As New HttpClient()
            Dim endTime = DateTime.Now.AddSeconds(expiresIn)
            While DateTime.Now < endTime
                Dim content As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                    {"grant_type", "urn:ietf:params:oauth:grant-type:device_code"},
                    {"client_id", ClientId},
                    {"device_code", deviceCode}
                })
                Dim resp = client.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", content).Result
                Dim json = JObject.Parse(resp.Content.ReadAsStringAsync().Result)

                If json("access_token") IsNot Nothing Then
                    Return json("access_token").ToString()
                End If

                Dim err = json("error")?.ToString()
                If err = "authorization_pending" Then
                    System.Threading.Thread.Sleep(interval * 1000)
                ElseIf err = "slow_down" Then
                    System.Threading.Thread.Sleep((interval + 5) * 1000)
                Else
                    Throw New Exception("Token 获取失败: " & err)
                End If
            End While
            Throw New Exception("设备代码已过期，请重试")
        End Using
    End Function

    ''' <summary>
    ''' 3. 微软 Token -> Xbox Live Token -> XSTS Token -> Minecraft Token
    ''' </summary>
    Public Function AuthenticateMinecraft(msAccessToken As String) As LoginResult
        Try
            Using client As New HttpClient()

                ' A. Xbox Live
                Dim xblPayload = New JObject From {
                    {"Properties", New JObject From {
                        {"AuthMethod", "RPS"},
                        {"SiteName", "user.auth.xboxlive.com"},
                        {"RpsTicket", msAccessToken}
                    }},
                    {"RelyingParty", "http://auth.xboxlive.com"},
                    {"TokenType", "JWT"}
                }
                Dim xblResp = client.PostAsync("https://user.auth.xboxlive.com/user/authenticate",
                    New StringContent(xblPayload.ToString(), System.Text.Encoding.UTF8, "application/json")).Result
                Dim xblJson = JObject.Parse(xblResp.Content.ReadAsStringAsync().Result)
                Dim xblToken = xblJson("Token")?.ToString()
                Dim uhs = xblJson("DisplayClaims")?("xui")?(0)?("uhs")?.ToString()

                If xblToken Is Nothing Then
                    Return New LoginResult With {.Success = False, .Message = "Xbox Live 认证失败"}
                End If

                ' B. XSTS
                Dim xstsPayload = New JObject From {
                    {"Properties", New JObject From {
                        {"SandboxId", "RETAIL"},
                        {"UserTokens", New JArray From {xblToken}}
                    }},
                    {"RelyingParty", "rp://api.minecraftservices.com/"},
                    {"TokenType", "JWT"}
                }
                Dim xstsResp = client.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize",
                    New StringContent(xstsPayload.ToString(), System.Text.Encoding.UTF8, "application/json")).Result
                Dim xstsJson = JObject.Parse(xstsResp.Content.ReadAsStringAsync().Result)
                Dim xstsToken = xstsJson("Token")?.ToString()

                If xstsToken Is Nothing Then
                    Return New LoginResult With {.Success = False, .Message = "XSTS 认证失败"}
                End If

                ' C. Minecraft Token
                Dim mcPayload = New JObject From {
                    {"identityToken", $"XBL3.0 x={uhs};{xstsToken}"}
                }
                Dim mcResp = client.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox",
                    New StringContent(mcPayload.ToString(), System.Text.Encoding.UTF8, "application/json")).Result
                Dim mcJson = JObject.Parse(mcResp.Content.ReadAsStringAsync().Result)
                Dim mcAccessToken = mcJson("access_token")?.ToString()

                If mcAccessToken Is Nothing Then
                    Return New LoginResult With {.Success = False, .Message = "Minecraft 认证失败"}
                End If

                ' D. 获取 Profile（UUID 和 玩家名）
                client.DefaultRequestHeaders.Clear()
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " & mcAccessToken)
                Dim profileResp = client.GetAsync("https://api.minecraftservices.com/minecraft/profile").Result
                Dim profileJson = JObject.Parse(profileResp.Content.ReadAsStringAsync().Result)

                Return New LoginResult With {
                    .Success = True,
                    .AccessToken = mcAccessToken,
                    .Uuid = profileJson("id")?.ToString(),
                    .PlayerName = profileJson("name")?.ToString(),
                    .Message = "登录成功"
                }
            End Using
        Catch ex As Exception
            Return New LoginResult With {.Success = False, .Message = $"登录过程出错: {ex.Message}"}
        End Try
    End Function

End Module
