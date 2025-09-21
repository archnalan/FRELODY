using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Constants
{
    public static class UserRoles
    {
        public const string Guest = "Guest";
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Manager = "Manager";
        public const string Contributor = "Contributor";
        public const string Editor = "Editor";
        public const string Moderator = "Moderator";
        public const string Owner = "Owner";
        public const string Viewer = "Viewer";

        public static readonly string[] AllRoles = 
        {
            Guest, Admin, User, Manager, 
            Contributor, Editor, Moderator, Owner, Viewer
        };
    }
}
