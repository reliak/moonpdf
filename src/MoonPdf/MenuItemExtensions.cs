/*! MoonPdf - A WPF-based PDF Viewer application that uses the MoonPdfLib library
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
 * 2013 - Modified version of Patrick's work (see AUTHORS file)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MoonPdf
{
	public class MenuItemExtensions : DependencyObject
	{
		public static Dictionary<MenuItem, String> ElementToGroupNames = new Dictionary<MenuItem, String>();

		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.RegisterAttached("GroupName",
										 typeof(String),
										 typeof(MenuItemExtensions),
										 new PropertyMetadata(String.Empty, OnGroupNameChanged));

		public static void SetGroupName(MenuItem element, String value)
		{
			element.SetValue(GroupNameProperty, value);
		}

		public static String GetGroupName(MenuItem element)
		{
			return element.GetValue(GroupNameProperty).ToString();
		}

		private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//Add an entry to the group name collection
			var menuItem = d as MenuItem;

			if (menuItem != null)
			{
				String newGroupName = e.NewValue.ToString();
				String oldGroupName = e.OldValue.ToString();
				if (String.IsNullOrEmpty(newGroupName))
				{
					//Removing the toggle button from grouping
					RemoveCheckboxFromGrouping(menuItem);
				}
				else
				{
					//Switching to a new group
					if (newGroupName != oldGroupName)
					{
						if (!String.IsNullOrEmpty(oldGroupName))
						{
							//Remove the old group mapping
							RemoveCheckboxFromGrouping(menuItem);
						}
						ElementToGroupNames.Add(menuItem, e.NewValue.ToString());
						menuItem.Checked += MenuItemChecked;
					}
				}
			}
		}

		private static void RemoveCheckboxFromGrouping(MenuItem checkBox)
		{
			ElementToGroupNames.Remove(checkBox);
			checkBox.Checked -= MenuItemChecked;
		}


		static void MenuItemChecked(object sender, RoutedEventArgs e)
		{
			var menuItem = e.OriginalSource as MenuItem;
			foreach (var item in ElementToGroupNames)
			{
				if (item.Key != menuItem && item.Value == GetGroupName(menuItem))
				{
					item.Key.IsChecked = false;
				}
			}
		}
	}
}
