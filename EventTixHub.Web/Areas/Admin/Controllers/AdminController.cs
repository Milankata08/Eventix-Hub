using EventTixHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

// Base of every Admin-area controller: area route + administrator-only access.
[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
public abstract class AdminController : Controller;
