/*! MoonPdfLib - Provides a WPF user control to display PDF files
Copyright (C) 2013  (see AUTHORS file)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
!*/
/*
 * 2013 - Modified version of Paul McClean's code (see AUTHORS file)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonPdfLib.Virtualizing
{
	/// <summary>
	/// Represents a provider of collection details.
	/// </summary>
	/// <typeparam name="T">The type of items in the collection.</typeparam>
	internal interface IItemsProvider<T>
	{
		/// <summary>
		/// Fetches the total number of items available.
		/// </summary>
		/// <returns></returns>
		int FetchCount();

		/// <summary>
		/// Fetches a range of items.
		/// </summary>
		/// <param name="startIndex">The start index.</param>
		/// <param name="count">The number of items to fetch.</param>
		/// <returns></returns>
		IList<T> FetchRange(int startIndex, int count);
	}
}
