using System;
using System.Linq;
using System.Collections.Generic;
using EcommerceApi.Models.DTOs;

namespace EcommerceApi.Models.Extensions
{
    public static class CartExtensions
    {
        public static CartDto ToDto(this Cart cart, string baseUrl)
        {
            if (cart == null) return null;

            var cartDto = CartDto.FromCart(cart);
            
            // Update image paths with full URLs
            if (cartDto.CartItems != null)
            {
                foreach (var item in cartDto.CartItems)
                {
                    item.ImagePath = GetFullImageUrl(item.ImagePath, baseUrl);
                }
            }

            return cartDto;
        }

        private static string GetFullImageUrl(string imagePath, string baseUrl)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            // If it's already a full URL, return as is
            if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
                return imagePath;

            // If it's a relative path starting with ~, remove it
            imagePath = imagePath.TrimStart('~', '/');

            // For wwwroot/images path
            if (!imagePath.StartsWith("images/"))
            {
                imagePath = $"images/{imagePath}";
            }

            // Combine base URL with image path
            return $"{baseUrl.TrimEnd('/')}/{imagePath}";
        }
    }
}
