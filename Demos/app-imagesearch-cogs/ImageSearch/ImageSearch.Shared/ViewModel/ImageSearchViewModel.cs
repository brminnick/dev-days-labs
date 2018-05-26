﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Acr.UserDialogs;

using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;

using MvvmHelpers;

using Plugin.Connectivity;

using ImageSearch.Services;

namespace ImageSearch.ViewModel
{
	public class ImageSearchViewModel
	{
		public ObservableRangeCollection<ImageObject> Images { get; } = new ObservableRangeCollection<ImageObject>();

		public async Task<bool> SearchForImagesAsync(string query)
		{
			if (!CrossConnectivity.Current.IsConnected)
			{
				await UserDialogs.Instance.AlertAsync("Not connected to the internet, please check connection.");
				return false;
			}

			try
			{
				var images = await ImageSearchServices.GetImage(query).ConfigureAwait(false);

				Images.ReplaceRange(images?.Value.Where(x => !(x.ContentUrl is null)));

				return true;
			}
			catch (Exception ex)
			{
				await UserDialogs.Instance.AlertAsync("Unable to query images: " + ex.Message);
				return false;
			}
		}
	}
}