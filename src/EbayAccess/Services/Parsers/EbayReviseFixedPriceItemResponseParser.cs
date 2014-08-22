﻿using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using EbayAccess.Misc;
using EbayAccess.Models.ReviseFixedPriceItemResponse;

namespace EbayAccess.Services.Parsers
{
	public class EbayReviseFixedPriceItemResponseParser : EbayXmlParser< ReviseFixedPriceItemResponse >
	{
		public ReviseFixedPriceItemResponse Parse( Stream stream )
		{
			try
			{
				XNamespace ns = "urn:ebay:apis:eBLBaseComponents";

				var root = XElement.Load( stream );

				ReviseFixedPriceItemResponse inventoryStatusResponse = null;

				var error = this.ResponseContainsErrors( root, ns );
				if( error != null )
					return new ReviseFixedPriceItemResponse() { Error = error };

				inventoryStatusResponse = new ReviseFixedPriceItemResponse();

				inventoryStatusResponse.Item.ItemId = GetElementValue( root, ns, "ItemID" ).ToLong();

				return inventoryStatusResponse;
			}
			catch( Exception ex )
			{
				var buffer = new byte[ stream.Length ];
				stream.Read( buffer, 0, ( int )stream.Length );
				var utf8Encoding = new UTF8Encoding();
				var bufferStr = utf8Encoding.GetString( buffer );
				throw new Exception( "Can't parse: " + bufferStr, ex );
			}
		}
	}
}