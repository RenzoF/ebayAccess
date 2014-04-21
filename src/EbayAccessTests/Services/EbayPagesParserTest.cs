﻿using System.IO;
using EbayAccess.Services.Parsers;
using FluentAssertions;
using NUnit.Framework;

namespace EbayAccessTests.Services
{
	[ TestFixture ]
	public class EbayPagesParserTest
	{
		[ Test ]
		public void ParsePaginationResultResponse_ResultContainsMultiplePages_AllPagesHandled()
		{
			//A
			using(
				var fs = new FileStream( @".\Files\EbayServiceGetSellerListResponseWith1PageOf4Contains1Item.xml", FileMode.Open, FileAccess.Read ) )
			{
				const int itemCount = 4;
				const int pagesCount = 4;

				//A
				var orders = new EbayPagesParser().ParsePaginationResultResponse( fs );

				//A
				orders.TotalNumberOfEntries.Should().Be( itemCount, "because source file contains record about {0} items", itemCount );
				orders.TotalNumberOfPages.Should().Be( pagesCount, "because source file contains record about {0} pages", pagesCount );
			}
		}
	}
}