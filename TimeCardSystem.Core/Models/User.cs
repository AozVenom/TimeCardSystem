using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TimeCardSystem.Core.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; } = UserRole.Employee;
        public decimal? HourlyRate { get; set; }

        // Employee ID property
        public int EmployeeId { get; set; }

        // Generate EmployeeID based on first name, last name and creation date
        [NotMapped]
        public string EmployeeCode
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
                    return string.Empty;

                // Get first 3 letters of first name (or fewer if name is shorter)
                string firstNamePart = FirstName.Length >= 3 ?
                    FirstName.Substring(0, 3).ToUpper() :
                    FirstName.PadRight(3, 'X').ToUpper();

                // Get first 3 letters of last name (or fewer if name is shorter)
                string lastNamePart = LastName.Length >= 3 ?
                    LastName.Substring(0, 3).ToUpper() :
                    LastName.PadRight(3, 'X').ToUpper();

                // Convert name parts to digits (A=1, B=2, etc.)
                string digitFirstName = ConvertLettersToDigits(firstNamePart);
                string digitLastName = ConvertLettersToDigits(lastNamePart);

                // Add creation date in YYMMDD format
                string datePart = CreatedAt.ToString("yyMMdd");

                return $"{digitFirstName}{digitLastName}{datePart}";
            }
        }

        // Helper method to convert letters to digits (A=1, B=2, etc.)
        private string ConvertLettersToDigits(string input)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    // A=1, B=2, etc.
                    int digit = char.ToUpper(c) - 'A' + 1;
                    // Ensure single digit (use modulo to keep within 1-9 range)
                    digit = (digit % 9) + 1;
                    result.Append(digit);
                }
                else if (char.IsDigit(c))
                {
                    result.Append(c);
                }
                else
                {
                    // For non-alphanumeric, use 0
                    result.Append('0');
                }
            }
            return result.ToString();
        }
    }

    public enum UserRole
    {
        Employee = 1,
        Manager = 2,
        Administrator = 3
    }
}