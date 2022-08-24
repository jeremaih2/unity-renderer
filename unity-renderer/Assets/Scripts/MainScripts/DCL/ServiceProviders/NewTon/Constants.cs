namespace DCL.Helpers.NFT.Markets.Newton_Internal
{
    internal static class Constants
    {
        //public const string SINGLE_ASSET_URL = "https://api.opensea.io/api/v1/asset";
        //public const string MULTIPLE_ASSETS_URL = "https://api.opensea.io/api/v1/assets";
        //public const string OWNED_ASSETS_URL = "https://api.opensea.io/api/v1/assets?owner=";
        public const string SINGLE_ASSET_URL = "https://api.devnet.andverse.org/api/v1/nft/asset";
        public const string MULTIPLE_ASSETS_URL = "https://api.devnet.andverse.org/api/v1/nft/assets";
        public const string OWNED_ASSETS_URL = "https://api.devnet.andverse.org/api/v1/nft/assets?asset_contract_addresses=";
        public const int REQUESTS_RETRY_ATTEMPS = 3;//尝试请求次数
    }
}