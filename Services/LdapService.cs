using Novell.Directory.Ldap;

namespace JobOnlineAPI.Services
{
    public class LdapService : ILdapService
    {
        private readonly IConfiguration _configuration;

        public LdapService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<bool> Authenticate(string username, string password)
        {
            var ldapServers = _configuration.GetSection("LdapServers").Get<List<LdapServer>>();

            if (ldapServers != null)
            {
                foreach (var server in ldapServers)
                {
                    try
                    {
                        using var connection = new LdapConnection();

                        var uri = new Uri(server.Url);
                        var host = uri.Host;
                        var port = uri.Port;

                        Console.WriteLine($"Connecting to {host}:{port}");
                        connection.Connect(host, port);
                        connection.Bind(server.BindDn, server.BindPassword);
                        Console.WriteLine("LDAP Connection and Bind successful.");

                        var searchFilter = $"(&(sAMAccountName={username})(objectClass=person))";
                        var searchResults = connection.Search(
                            server.BaseDn,
                            LdapConnection.ScopeSub,
                            searchFilter,
                            null,
                            false
                        );

                        while (searchResults.HasMore())
                        {
                            var entry = searchResults.Next();
                            if (entry is LdapEntry ldapEntry)
                            {
                                var userDn = ldapEntry.Dn;

                                using var userConnection = new LdapConnection();
                                userConnection.Connect(host, port);
                                userConnection.Bind(userDn, password);

                                Console.WriteLine($"LDAP Authentication successful for user {username}");
                                return Task.FromResult(true);
                            }
                        }
                    }
                    catch (LdapException ex)
                    {
                        Console.WriteLine($"LDAP Error for server {server.Url}: {ex.Message}");
                        continue;
                    }
                }
            }

            return Task.FromResult(false);
        }
    }

    public class LdapServer
    {
        public string Url { get; set; } = string.Empty;
        public string BindDn { get; set; } = string.Empty;
        public string BindPassword { get; set; } = string.Empty;
        public string BaseDn { get; set; } = string.Empty;
    }
}