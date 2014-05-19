﻿using NUnit.Framework;

namespace EbayAccessTests.Integration.TestEnvironment
{
	[ TestFixture ]
	public abstract class TestBase
	{
		protected TestCredentials _credentials;

		protected string FilesEbayTestCredentialsCsv = @"..\..\Files\ebay_test_credentials.csv";
		protected string FilesEbayTestDevcredentialsCsv = @"..\..\Files\ebay_test_devcredentials.csv";
		protected string FilesEbayTestSaleitemsidsCsv = @"..\..\Files\ebay_test_saleitemsids.csv";
		protected string FilesEbayTestRunameCsv = @"..\..\Files\ebay_test_runame.csv";

		[ SetUp ]
		public void Init()
		{
			this._credentials = new TestCredentials( this.FilesEbayTestCredentialsCsv, this.FilesEbayTestDevcredentialsCsv, this.FilesEbayTestSaleitemsidsCsv, this.FilesEbayTestRunameCsv );
		}
	}

	public static class TestItemsDescriptions

	{
		public static string AnyExistingNonVariationItem { get; set; }
		public static string ExistingItemWithVariationsSku { get; set; }

		static TestItemsDescriptions()
		{
			AnyExistingNonVariationItem = "AnyExistingNonVariationItem";
			ExistingItemWithVariationsSku = "ExistingItemWithVariationsSku";
		}
	}
}