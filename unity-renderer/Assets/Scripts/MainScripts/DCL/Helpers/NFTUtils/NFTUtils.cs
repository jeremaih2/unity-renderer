using System;
using System.Collections;
using DCL.Helpers.NFT.Markets;

namespace DCL.Helpers.NFT
{
    public static class NFTUtils
    {
        /// <summary>
        /// Fetch NFT from owner
        /// </summary>
        /// <param name="address">owner address</param>
        /// <param name="onSuccess">success callback</param>
        /// <param name="onError">error callback</param>
        /// <returns>IEnumerator</returns>
        public static IEnumerator FetchNFTsFromOwner(string darURLProtocol, string address, Action<NFTOwner> onSuccess, Action<string> onError)
        {
            INFTMarket selectedMarket = null;
            INFTMarket selectedMarket_Newton = null;
            if (darURLProtocol == "ethereum")
            {
                yield return GetMarket(darURLProtocol, address, (mkt) => selectedMarket = mkt);
            }
            if (darURLProtocol == "newton")
            {
                yield return GetMarket(darURLProtocol, address, (mkt_newton) => selectedMarket_Newton = mkt_newton);
            }
            

            if (selectedMarket != null)
            {
                yield return selectedMarket.FetchNFTsFromOwner(address, onSuccess, onError);
            }
            else if (selectedMarket_Newton != null)
            {
                yield return selectedMarket_Newton.FetchNFTsFromOwner(address, onSuccess, onError);
            }
            else
            {
                onError?.Invoke($"Market not found for asset {address}");
            }
        }

        /// <summary>
        /// Fetch NFT. Request is added to a batch of requests to reduce the amount of request to the api.
        /// NOTE: for ERC1155 result does not contain the array of owners and sell price for this asset
        /// </summary>
        /// <param name="assetContractAddress">asset contract address</param>
        /// <param name="tokenId">asset token id</param>
        /// <param name="onSuccess">success callback</param>
        /// <param name="onError">error callback</param>
        /// <returns>IEnumerator</returns>
        public static IEnumerator FetchNFTInfo(string darURLProtocol,string assetContractAddress, string tokenId, Action<NFTInfo> onSuccess, Action<string> onError)
        {
            //INFTMarket_Newton selectedMarket_Newton = null;
            INFTMarket selectedMarket = null;
            INFTMarket selectedMarket_Newton = null;
            if (darURLProtocol == "ethereum")
            {
                yield return GetMarket(darURLProtocol, assetContractAddress, tokenId, (mkt) => selectedMarket = mkt);
            }
            if (darURLProtocol == "newton")
            {
                yield return GetMarket(darURLProtocol, assetContractAddress, tokenId, (selectedMarket_Newton) => selectedMarket = selectedMarket_Newton);
            }

            if (selectedMarket != null)
            {
                yield return selectedMarket.FetchNFTInfo(assetContractAddress, tokenId, onSuccess, onError);
            }
            else if (selectedMarket_Newton != null)
            {
                yield return selectedMarket_Newton.FetchNFTInfo(assetContractAddress, tokenId, onSuccess, onError);
            }
            else
            {
                onError?.Invoke($"Market_Newton not found for asset {assetContractAddress}/{tokenId}");
            }
        }

        /// <summary>
        /// Fetch NFT. Request is fetch directly to the api instead of batched with other requests in a single query.
        /// Please try to use `FetchNFTInfo` if ownership info is not relevant for your use case.
        /// NOTE: result it does contain the array of owners for ERC1155 NFTs
        /// </summary>
        /// <param name="assetContractAddress">asset contract address</param>
        /// <param name="tokenId">asset token id</param>
        /// <param name="onSuccess">success callback</param>
        /// <param name="onError">error callback</param>
        /// <returns>IEnumerator</returns>
        public static IEnumerator FetchNFTInfoSingleAsset(string darURLProtocol, string assetContractAddress, string tokenId, Action<NFTInfoSingleAsset> onSuccess, Action<string> onError)
        {
            INFTMarket selectedMarket = null;
            INFTMarket selectedMarket_Newton = null;
            if (darURLProtocol == "ethereum")
            {
                yield return GetMarket(darURLProtocol, assetContractAddress, tokenId, (mkt) => selectedMarket = mkt);
            }
            if (darURLProtocol == "newton")
            {
                yield return GetMarket(darURLProtocol, assetContractAddress, tokenId, (mkt_newton) => selectedMarket_Newton = mkt_newton);
            }

            if (selectedMarket != null)
            {
                yield return selectedMarket.FetchNFTInfoSingleAsset(assetContractAddress, tokenId, onSuccess, onError);
            }
            else if (selectedMarket_Newton != null)
            {
                yield return selectedMarket_Newton.FetchNFTInfoSingleAsset(assetContractAddress, tokenId, onSuccess, onError);
            }
            else
            {
                onError?.Invoke($"Market not found for asset {assetContractAddress}/{tokenId}");
            }
        }

        // NOTE: this method doesn't make sense now, but it will when support for other market is added
        //这种方法现在没有意义，但当支持其他市场时，它会
        public static IEnumerator GetMarket(string darURLProtocol, string assetContractAddress, string tokenId, Action<INFTMarket> onSuccess)
        {
            IServiceProviders serviceProviders = Environment.i.platform.serviceProviders;
            INFTMarket openSea = null;
            INFTMarket newMall = null;

            if ( serviceProviders != null )
            {
                if (darURLProtocol == "ethereum")
                {
                    openSea = serviceProviders.openSea;
                    onSuccess?.Invoke(openSea);
                }
                else
                {
                    newMall = serviceProviders.newton;
                    onSuccess?.Invoke(newMall);
                }
                
            }
            yield break;
        }

        public static IEnumerator GetMarket(string darURLProtocol, string assetContractAddress, Action<INFTMarket> onSuccess)
        {
            IServiceProviders serviceProviders = Environment.i.platform.serviceProviders;
            INFTMarket openSea = null;
            INFTMarket newMall = null;

            if ( serviceProviders != null )
            {
                if (darURLProtocol == "ethereum")
                {
                    openSea = serviceProviders.openSea;
                    onSuccess?.Invoke(openSea);
                }
                else
                {
                    newMall = serviceProviders.newton;
                    onSuccess?.Invoke(newMall);
                }
            }
            yield break;
        }

        //public static IEnumerator GetMarket_Newton(string assetContractAddress, string tokenId, Action<INFTMarket> onSuccess)
        //{
        //    IServiceProviders serviceProviders = Environment.i.platform.serviceProviders;
        //    INFTMarket_Newton newton = null;

        //    if (serviceProviders != null)
        //        newton = serviceProviders.newton;

        //    onSuccess?.Invoke(newton);
        //    yield break;
        //}
    }
}