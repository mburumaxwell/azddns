# azddns – Azure Dynamic DNS CLI Tool

A CLI tool to update Azure DNS `A` or `AAAA` records with the current public IP address of the machine it runs on (e.g., a Pi, dev laptop, container, or VM). Intended for use cases like keeping dynamic IPs updated in Azure DNS so they can be referenced in firewall rules or other infrastructure.

Keeps records like `office.maxwellweru.io` or `office.contoso.com` up-to-date with the current public IP of dynamic endpoints. This consequently enables firewall rules to allow access based on hostname/IPs synced via [azfwman](https://github.com/mburumaxwell/azfwrmgr).

IP information sourced from [ipify](https://www.ipify.org) using <https://api64.ipify.org?format=json>

## ✅ Futures

- Support IPv6 alongside IPv4 (i.e. `AAAA` and `A` records).
- Support for dry run (useful to see if it will work as expected).
- Check current IP to prevent uncessary updates.
- Runs on **headless devices** (e.g., Raspberry Pi) and in **automated environments** (e.g., cron jobs, ACA, AKS, ACI).
- Support homebrew, scoop, docker and standalone binaries.

## 🚀 CLI Usage

### 1. Interactive / Developer Mode

```bash
azddns update \
  --zone maxwellweru.io \
  --record office \
  --resource-group infra \
  --subscription personal \
  --ttl 3600 \
  --interactive \
  --dry-run
```

- Uses `DefaultAzureCredential` with interactive browser login allowed.
- Useful on dev laptops where `az login` has already been run.

### 2. Headless / Automated Mode

```bash
azddns run --config ~/.az-ddns/config.json
```

- Uses config file instead of CLI args.
- Designed for headless environments like:
  - Raspberry Pi
  - GitHub Actions
  - Azure Container Apps / AKS / ACI
  - `systemd` services

#### ⚙️ Config File Format (`config.json`)

```json
{
  "subscription": "personal",
  "resourceGroup": "infra",
  "zoneName": "maxwellweru.io",
  "recordName": "office",
  "ttl": 3600,
  "interval": 900,
  "dryRun": false
}
```

- **subscription**: Azure subscription ID or name.
- **resourceGroup**: Azure resource group for the DNS zone.
- **zoneName**: DNS zone name.
- **recordName**: A/AAAA record to update.
- **ttl**: Optional TTL (time-to-live) in seconds for the record (default: 3600).
- **interval**: Optional interval in seconds to check for changes (default: 900).
- **dryRun**: Optionally test the logic without actually updating the DNS records. (default: false).

## 🔐 Authentication Strategy

Authentication is handled using **Azure.Identity**'s `DefaultAzureCredential`. It chains multiple sources as described in the [official docs](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains?tabs=dac)

### Service Principal (preferred for headless)

```bash
export AZURE_TENANT_ID=ttt
export AZURE_CLIENT_ID=ccc
export AZURE_CLIENT_SECRET=sss
export AZURE_SUBSCRIPTION_ID=sss
azddns run --config ~/.az-ddns/config.json
```

> A managed identity is basically a service principal that you use without having to manage the credentials. System assigned managed identities are simple and no further configuration is required. For User assigned managed identity, you only need to set the `AZURE_CLIENT_ID` environment variable to disambiguate from any other being used by the platform such as when using ACA jobs.

## Alternatives

There are quite a number of alternatives but nothing quite matched what I needed. This is what I looked at:

- <https://github.com/TechJosh/AzureDynamicDNS>
- <https://github.com/FrodeHus/azure-dyndns>
- <https://github.com/PatrickTCB/azure-ddns>
- <https://github.com/izpavlovich/azure-dns-update>
- <https://github.com/dewhurstwill/azure-ddns>
- <https://github.com/danimart1991/azure-dns-updater>
- <https://github.com/esqew/azure-dynamic-dns>
- <https://github.com/cgreenza/AzureDynamicDns>
- <https://github.com/jeff-winn/azure-ddns>
- <https://github.com/mplogas/azure_dnsupdater>
- <https://github.com/stonekw/azure-ddns>
- <https://github.com/KelvinTegelaar/AzDynaDNS>

### License

The Library is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](./LICENSE) file for more information.
