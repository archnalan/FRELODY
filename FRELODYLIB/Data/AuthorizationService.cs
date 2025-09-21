using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Newtonsoft.Json;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYAPP.Extensions;
using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Hosting;
using FRELODYLIB.ServiceHandler.ResultModels;
using Mapster;

namespace FRELODYAPP.Data
{
    public class AuthorizationService : IAuthorizationService
    {
        private IConfiguration _config;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly SongDbContext _context;
        private readonly SmtpSenderService _emailSmtpService;
        private readonly ILogger<AuthorizationService> _logger;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly FileValidationService _fileValidationService;
        private readonly SecurityUtilityService _securityUtilityService;
        private readonly TokenService _tokenService;

        public AuthorizationService(IConfiguration config, SongDbContext context,
            SignInManager<User> signInManager, UserManager<User> userManager,
            IWebHostEnvironment webHostEnvironment, SmtpSenderService emailSmtpService,
            ILogger<AuthorizationService> logger, ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor, RoleManager<IdentityRole> roleManager,
            FileValidationService fileValidationService, SecurityUtilityService securityUtilityService, TokenService tokenService)
        {
            _config = config;
            _signInManager = signInManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _emailSmtpService = emailSmtpService;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleManager = roleManager;
            _fileValidationService = fileValidationService;
            _securityUtilityService = securityUtilityService;
            _tokenService = tokenService;
        }

        public async Task<ServiceResult<string>> InitiatePasswordReset(string emailAddress)
        {
            try
            {
                var rateLimitResult = _securityUtilityService.CheckRateLimit(emailAddress, 3, TimeSpan.FromMinutes(5));
                if (rateLimitResult == false)
                {
                    return ServiceResult<string>.Failure(
                        new TooManyRequestsException("Too many password reset attempts. Please try again after 5 minutes."));
                }
                var allowedDomains = _config.GetSection("AllowedOrigins").Get<string[]>();

                var requestUrl = _httpContextAccessor.HttpContext?.Request.HttpContext.Request.Headers.Origin;
                var isProd = _webHostEnvironment.IsProduction();
                if ((isProd && string.IsNullOrEmpty(requestUrl)) || allowedDomains.Where(x => x.Contains(requestUrl.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).Count() == 0)
                {
                    //not authorized domain
                    return ServiceResult<string>.Failure(new ForbiddenException("Not Authorized Origin"));
                }

                var user = await _userManager.FindByEmailAsync(emailAddress);
                if (user == null) return ServiceResult<string>.Failure(new NotFoundException("User does not exist"));
                _emailSmtpService.sendPasswordResetEmail(user, requestUrl);

                //log security event
                await _securityUtilityService.LogSecurityEvent(user.Id,
                "PasswordResetInitiated",
                $"Password reset requested for {emailAddress}",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                );

                return ServiceResult<string>.Success("Reset Email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while initiating initiating password reset. {ex}", ex);

                return ServiceResult<string>.Failure(new ServerErrorException("Error while initiating initiating password reset. Please try again later"));

            }

        }

        public async Task<ServiceResult<List<ComboBoxDto>>> GetUsersForComboBoxes()
        {
            try
            {
                var result = await _context.Users.AsNoTracking().Select(x => new ComboBoxDto
                {
                    IdString = x.Id,
                    ValueText = $"{x.FirstName ?? ""} {x.LastName ?? ""}"

                }).ToListAsync();
                return ServiceResult<List<ComboBoxDto>>.Success(result);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error while getting users for combo boxes. {ex}", ex);
                return ServiceResult<List<ComboBoxDto>>.Failure(new ServerErrorException("Error while getting users for combo boxes. Please try again later"));
            }
        }

        //OAuth
        public async Task<ServiceResult<LoginResponseDto>> ExternalLoginCallback(string code, string tenantId)
        {
            var client = new HttpClient();
            var uri = new Uri("https://oauth2.googleapis.com/token");
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", $"{_config["GoogleAuth:client_id"]}" },
                { "client_secret", $"{_config["GoogleAuth:client_secret"]}" },
                { "redirect_uri", $"{_config["GoogleAuth:redirectUri"]}" },
                { "grant_type", "authorization_code" }
            });

            var response = await client.PostAsync(uri, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Check the status code and response content for error handling
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Parse the response content into a .NET object
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

                    // Use the access token or refresh token as needed
                    var accessToken = tokenResponse.Access_token;
                    var refreshToken = tokenResponse.Refresh_token;

                    var settings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _config["GoogleAuth:client_id"] }
                    };
                    var objFromGoogle = await GoogleJsonWebSignature.ValidateAsync(tokenResponse.Id_token, settings);
                    //check user exists
                    var userFromDb = await _userManager.FindByLoginAsync("google", objFromGoogle.Subject);
                    // Update token generation to use TokenService
                    if (userFromDb != null)
                    {
                        var tokens = await _tokenService.GenerateTokens(userFromDb, tenantId);
                        await _securityUtilityService.LogSecurityEvent(
                            userFromDb.Id,
                            "ExternalLogin",
                            "Successful Google login",
                            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                        );
                        return ServiceResult<LoginResponseDto>.Success(tokens);
                    }
                    IdentityResult result;
                    if (userFromDb == null)
                    {
                        //check user exists
                        userFromDb = await _userManager.FindByEmailAsync(objFromGoogle.Email);
                        if (userFromDb != null)
                        {
                            result = await _userManager.AddLoginAsync(userFromDb, new UserLoginInfo("google", objFromGoogle.Subject, objFromGoogle.Name));
                            return await LoginUserNoPassword(userFromDb, tenantId);
                        }
                        //Created user
                        var newUser = new User()
                        {
                            Email = objFromGoogle.Email,
                            UserName = objFromGoogle.Name.ToLower().Replace(" ", "") + "-" + Guid.NewGuid().ToString().Substring(1, 3),
                            FirstName = objFromGoogle.Name.Split(" ").FirstOrDefault(),
                            LastName = objFromGoogle.Name.Split(" ")?[1],
                            EmailConfirmed = objFromGoogle.EmailVerified,
                            ProfilePicUrl = objFromGoogle.Picture,

                        };
                        var strategy = _context.Database.CreateExecutionStrategy();

                        return await strategy.ExecuteAsync(async () =>
                        {

                            using (var scope = _context.Database.BeginTransaction())
                            {
                                result = await _userManager.CreateAsync(newUser);
                                var userLoginInfo = new UserLoginInfo("google", objFromGoogle.Subject, objFromGoogle.Name);
                                result = await _userManager.AddLoginAsync(newUser, userLoginInfo);
                                if (result.Succeeded)
                                {
                                    //if all is well
                                    scope.Commit();
                                    return await LoginUserNoPassword(newUser, tenantId);
                                }

                                var outputErrors = new List<string>();
                                foreach (var error in result.Errors)
                                {
                                    outputErrors.Add(error.Description);
                                }
                                return ServiceResult<LoginResponseDto>.Failure(new BadRequestException(string.Join("\n", outputErrors)));

                            }

                        });


                    }
                    else
                    {
                        //User already exisit as external login. sign in
                        return await LoginUserNoPassword(userFromDb, tenantId);
                    }




                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occured while signing in with google Message: {ex.Message} and detail :{JsonConvert.SerializeObject(ex)}");
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Failed to Autheticate. Please try again later"));
                }
            }
            else
            {
                _logger.LogError($"An error occured while signing in with google. the request returned an error. Responsecontent:  :{JsonConvert.SerializeObject(responseContent)}");
                return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Failed to Autheticate. Please try again later"));

            }
        }

        public async Task<ServiceResult<LoginResponseDto>> Login(UserLogin userLogin)
        {
            try
            {
                var rateLimitResult = _securityUtilityService.CheckLoginRateLimit(userLogin.Email);
                if (!rateLimitResult.IsSuccess)
                    return ServiceResult<LoginResponseDto>.Failure(rateLimitResult.Error);

                var user = await Authenticate(userLogin);
                if (user == null)
                {
                    await Task.Delay(500); // Timing attack mitigation
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Invalid username or password"));
                }
                else
                {
                    if (user.TenantId == null) return ServiceResult<LoginResponseDto>.Failure(
                        new BadRequestException($"Invalid Tenant. contact {_config["ApplicationInfo:SupportEmail"]} for support"));

                    var token = await _tokenService.GenerateTokens(user, user.TenantId);

                    // Reset login attempts on successful login
                    _securityUtilityService.ResetLoginAttempts(userLogin.Email);

                    // Log security event
                    await _securityUtilityService.LogSecurityEvent(
                        user.Id,
                        "Login",
                        "Successful login",
                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                    );

                    // Send email notification (async)
                    await SendLoginNotification(user);
                    
                    //TODO: Check Browser cookie for new devices before sending Email!
                    
                    return ServiceResult<LoginResponseDto>.Success(token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Login error: {ex}", ex);
                return ServiceResult<LoginResponseDto>.Failure(new ServerErrorException("Login failed"));
            }
        }

        public async Task<ServiceResult<LoginResponseDto>> LoginUserNameOrPhone(
            [OneRequiredProp(new string[] { "UserName", "PhoneNumber" })] LoginUserNameOrPhoneDto userLogin, string tenantId)
        {
            try
            {
                var identifier = userLogin.UserName ?? userLogin.PhoneNumber;
                var rateLimitResult = _securityUtilityService.CheckLoginRateLimit(identifier!);
                if (!rateLimitResult.IsSuccess)
                    return ServiceResult<LoginResponseDto>.Failure(rateLimitResult.Error);

                var userResult = await AuthenticateByUserNameOrPhone(userLogin);
                if (!userResult.IsSuccess)
                    return ServiceResult<LoginResponseDto>.Failure(userResult.Error);

                var user = userResult.Data;

                var tenantExists = await _context.Tenants.AnyAsync(x => x.Id == tenantId);

                if (!tenantExists)
                    return ServiceResult<LoginResponseDto>.Failure(
                        new BadRequestException($"Invalid Tenant Id. Contact {_config["ApplicationInfo:SupportUrl"]} for support."));

                var token = await _tokenService.GenerateTokens(user, tenantId);

                _securityUtilityService.ResetLoginAttempts(identifier!);

                await _securityUtilityService.LogSecurityEvent(
                user.Id,
                "Login",
                "Successful username/phone login",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                );

                await SendLoginNotification(user);
                
                return ServiceResult<LoginResponseDto>.Success(token);

            }
            catch (Exception ex)
            {
                _logger.LogError("Login error: {ex}", ex);
                return ServiceResult<LoginResponseDto>.Failure(new ServerErrorException("Login failed"));
            }
        }

        private async Task<ServiceResult<LoginResponseDto>> LoginUserNoPassword(User userFromDb, string tenantId)
        {
            var token = await _tokenService.GenerateTokens(userFromDb, tenantId);

            var emailDto = new EmailDto(true);
            emailDto.Subject = "A new Login has been detected";
            emailDto.ToEmail = userFromDb.Email;
            emailDto.Body = $"A new login into your account with {_config["ApplicationInfo:Name"]} has been detetected at {DateTime.UtcNow}UTC. If this wasn't you, You can take some steps to secure your account such as changing your account password with us or contacting us for help. <br/><br/>If this was you, then you can Ignore this message.<br/><br/>{_config["ApplicationInfo:SupportUrl"]} ";

            _emailSmtpService.SendMail(emailDto);

            //return Login token
            return ServiceResult<LoginResponseDto>.Success(token);
        }

        public async Task<ServiceResult<CreateUserResponseDto>> CreateUser([Required] CreateUserDto createUserDto, string tenantId)
        {
            var tenantExists = await _context.Tenants.AnyAsync(x => x.Id == tenantId);

            if (!tenantExists) return ServiceResult<CreateUserResponseDto>.Failure(new NotFoundException($"Tenant with ID:{tenantId} is Invalid"));
            //using  trnsaction scope

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                return await CreateUserWithRoleAsync(createUserDto);
            });

            //submethod to allow breakpoint hitting in an arrow function
            async Task<ServiceResult<CreateUserResponseDto>> CreateUserWithRoleAsync([Required] CreateUserDto createUserDto)
            {
                var usernameExists = await _context.Users
                                    .Where(x => x.TenantId == tenantId)
                                    .AnyAsync(x => x.UserName == createUserDto.UserName);

                if (usernameExists)
                    return ServiceResult<CreateUserResponseDto>.Failure(
                        new BadRequestException($"Username: {createUserDto.UserName} already taken."));

                using (var scope = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        User newUser = createUserDto.Adapt<User>();
                        newUser.TenantId = tenantId;
                        var result = await _userManager.CreateAsync(newUser, createUserDto.Password);
                        if (result.Succeeded)
                        {
                            if(createUserDto.AssignedRoles != null)
                            {
                                foreach (var roleName in createUserDto.AssignedRoles)
                                {

                                    var role = await AddUserToRoleAsync(newUser.Id, roleName);
                                    if (!role.IsSuccess)
                                    {
                                        //failed to add one of the roles
                                        await scope.RollbackAsync();
                                        return ServiceResult<CreateUserResponseDto>.Failure(role.Error);
                                    }
                                }
                                await scope.CommitAsync();

                                var tokens = await _tokenService.GenerateTokens(newUser, tenantId);
                                var output = newUser.Adapt<CreateUserResponseDto>();
                                //output. = tokens.Token; // Assuming CreateUserResponseDto has this property
                                return ServiceResult<CreateUserResponseDto>.Success(output);
                            }
                           
                        }

                        await scope.RollbackAsync();
                        var outputErrors = new List<string>();
                        foreach (var error in result.Errors)
                        {
                            outputErrors.Add(error.Description);
                        }
                        return ServiceResult<CreateUserResponseDto>.Failure(new BadRequestException(string.Join("\n", outputErrors)));

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error while creating user. {ex}", ex);
                        return ServiceResult<CreateUserResponseDto>.Failure(new ServerErrorException("Error while creating user. Please try again later"));
                    }
                    
                }
            }
        }

        public async Task<ServiceResult<string>> ResetPassword(ResetPasswordDto resetPasswordDto)
        {


            if (string.IsNullOrEmpty(resetPasswordDto.OldPassword) && string.IsNullOrEmpty(resetPasswordDto.ResetToken))
            {
                return ServiceResult<string>.Failure(new BadRequestException("TokenContext and old password cannot all be null"));

            }
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.EmailAddress);
            if (user == null) return ServiceResult<string>.Failure(new NotFoundException("User doesnot exist"));
            if (resetPasswordDto?.OldPassword is not null)
            {

                var result = await _userManager.ChangePasswordAsync(user, resetPasswordDto.OldPassword, resetPasswordDto.Password);

                if (result.Succeeded)
                {
                    return ServiceResult<string>.Success("Password Changed Successfully");
                }
                else
                {
                    var outputErrors = new List<string>();
                    foreach (var error in result.Errors)
                    {
                        outputErrors.Add(error.Description);
                    }
                    return ServiceResult<string>.Failure(new BadRequestException(string.Join("\n", outputErrors)));

                }
            }
            else
            {
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDto.ResetToken));


                var result = await _userManager.ResetPasswordAsync(user, code, resetPasswordDto.Password);

                if (result.Succeeded)
                {
                    return ServiceResult<string>.Success("Password Changed Successfully");
                }
                else
                {
                    var outputErrors = new List<string>();
                    foreach (var error in result.Errors)
                    {
                        outputErrors.Add(error.Description);
                    }
                    return ServiceResult<string>.Failure(new BadRequestException(string.Join("\n", outputErrors)));

                }
            }

        }

        public async Task<ServiceResult<CreateUserResponseDto>> UpdateUser(UpdateUserProfile updateUserProfile)
        {
            //using  trnsaction scope

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                return await UpdateUserWithRoleAsync(updateUserProfile);
            });
            //submethod to allow breakpoint hitting in an arrow function
            async Task<ServiceResult<CreateUserResponseDto>> UpdateUserWithRoleAsync(UpdateUserProfile updateUserProfile)
            {
                using (var scope = await _context.Database.BeginTransactionAsync())
                {
                    var files = _httpContextAccessor.HttpContext?.Request.Form.Files;

                    //read user from user context
                    var currentUserId = _tenantProvider.GetCurrentUser()?.Id;
                    var objFromDb = await _userManager.FindByIdAsync(currentUserId); //TODO: get id from httpcontext

                    if (objFromDb == null) return ServiceResult<CreateUserResponseDto>.Failure(new NotFoundException("User does not exist"));


                    //save image if exists

                    if (files.Count() > 0)
                    {
                        //save dp if exists
                        var dp = files.Where(x => x.Name.ToLower() == "profilepic").ToArray();

                        if (dp != null && dp.Length > 0)
                        {
                            var fileResult = await _fileValidationService.ValidateFileAsync(dp.First());
                            if (!fileResult.IsValid)
                                return ServiceResult<CreateUserResponseDto>.Failure(new
                                    BadRequestException(fileResult.ErrorMessage));

                            var saveResult = await SaveFile(dp.First());
                            if (saveResult.ReturnCode == "200")
                            {
                                objFromDb.ProfilePicUrl = saveResult.Link;
                            }
                            else
                            {
                                return ServiceResult<CreateUserResponseDto>.Failure(new BadRequestException(saveResult.Message));

                            }

                        }
                        //save cover pic if exists
                        var cover = files.Where(x => x.Name.ToLower() == "coverpic").ToArray();
                        if (cover.Any())
                        {
                            var saveResult = await SaveFile(cover.FirstOrDefault());
                            if (saveResult.ReturnCode == "200")
                            {
                                objFromDb.CoverPhotoUrl = saveResult.Link;
                            }
                            else
                            {
                                return ServiceResult<CreateUserResponseDto>.Failure(new BadRequestException(saveResult.Message));

                            }
                        }

                    }

                    if (!string.IsNullOrEmpty(updateUserProfile.FirstName)) objFromDb.FirstName = updateUserProfile.FirstName;
                    if (!string.IsNullOrEmpty(updateUserProfile.LastName)) objFromDb.LastName = updateUserProfile.LastName;


                    objFromDb.AboutMe = updateUserProfile.AboutMe;
                    objFromDb.Contact = updateUserProfile.PhoneNumber;
                    objFromDb.Address = updateUserProfile.Address;
                    objFromDb.UserName = updateUserProfile.UserName;

                    //TODO: Updating an Email

                    var result = await _userManager.UpdateAsync(objFromDb);
                    if (result.Succeeded)
                    {
                        if (updateUserProfile.AssignedRoles is not null)
                        {
                            foreach (var roleName in updateUserProfile.AssignedRoles)
                            {

                                var role = await AddUserToRoleAsync(objFromDb.Id, roleName);
                                if (!role.IsSuccess)
                                {
                                    //failed to add one of the roles
                                    await scope.RollbackAsync();
                                    return ServiceResult<CreateUserResponseDto>.Failure(role.Error);
                                }
                            }
                            await scope.CommitAsync();
                            var output = objFromDb.Adapt<CreateUserResponseDto>();
                            return ServiceResult<CreateUserResponseDto>.Success(output);
                        }
                    }
                    var outputErrors = new List<string>();
                    foreach (var error in result.Errors)
                    {
                        outputErrors.Add(error.Description);
                    }
                    return ServiceResult<CreateUserResponseDto>.Failure(new BadRequestException(string.Join("\n", outputErrors)));

                }
            }




        }

        public async Task<ServiceResult<UpdateUserProfileOutDto>> GetUserProfile(string id = null, string userName = null)
        {
            if (id == null && userName == null)
            {
                return ServiceResult<UpdateUserProfileOutDto>.Failure(new BadRequestException("Both Username and Id cannot be null for this request"));

            }
            User user;
            if (!string.IsNullOrEmpty(userName) && userName != "null" && userName != "undefined")
            {
                user = await _userManager.FindByNameAsync(userName);
            }
            else
            {
                user = await _userManager.FindByIdAsync(id);
            }

            if (user == null) return ServiceResult<UpdateUserProfileOutDto>.Failure(new NotFoundException("User not found"));

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseLink = request != null ? $"{request?.Scheme}://{request?.Host.Value}/" : null;
            //var list = new List<CreatedPostOutDto>();

            var userProfile = user.Adapt<UpdateUserProfileOutDto>();

            userProfile.ProfilePicUrl = !string.IsNullOrEmpty(user.ProfilePicUrl) ? baseLink + user.ProfilePicUrl : "https://www.seekpng.com/png/detail/143-1435868_headshot-silhouette-person-placeholder.png";
            userProfile.CoverPhotoUrl = !string.IsNullOrEmpty(user.CoverPhotoUrl) ? baseLink + user.CoverPhotoUrl : "https://via.placeholder.com/728x500.png?text=No+Cover+Image";

            return ServiceResult<UpdateUserProfileOutDto>.Success(userProfile);
        }


        //private async Task<string> GenerateUserName(string oldUserName)
        //{
        //    int random = 1;
        //    string newUsername = oldUserName + random.ToString();

        //    while (await _userManager.FindByNameAsync(newUsername) != null)
        //    {
        //        random++;
        //        newUsername = oldUserName + random.ToString();
        //        //prevent  infinte loop
        //        if (random > 100)
        //        {
        //            break;
        //        }

        //    }
        //    return newUsername;
        //}
        public async Task<ServiceResult<string>> AddUserToRoleAsync(string userId, string roleName)
        {
            try
            {
                // Find the user by their ID
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return ServiceResult<string>.Failure(new NotFoundException("User not found."));
                }
                // Check if the role exists
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return ServiceResult<string>.Failure(new BadRequestException($"Role '{roleName}' does not exist."));
                }
                // Check if the user is already in the role
                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (isInRole)
                {
                    return ServiceResult<string>.Failure(new BadRequestException("User is already in the specified role."));
                }

                // Add the user to the role
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    return ServiceResult<string>.Success("User successfully added to the role.");
                }

                // If role addition failed, concatenate the errors
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult<string>.Failure(new ServerErrorException($"Failed to add user to role. Errors: {errors}"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding user to role. {ex}", ex);
                return ServiceResult<string>.Failure(new ServerErrorException("An error occurred while adding user to role. Please try again later."));
            }
        }

        public async Task<ServiceResult<string>> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            try
            {
                // Find the user by their ID
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return ServiceResult<string>.Failure(new NotFoundException("User not found."));
                }
                // Check if the role exists
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return ServiceResult<string>.Failure(new BadRequestException($"Role '{roleName}' does not exist."));
                }
                // Check if the user is in the role
                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (!isInRole)
                {
                    return ServiceResult<string>.Failure(new BadRequestException("User is not in the specified role."));
                }

                // Remove the user from the role
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    return ServiceResult<string>.Success("User successfully removed from the role.");
                }

                // If role removal failed, concatenate the errors
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult<string>.Failure(new ServerErrorException($"Failed to remove user from role. Errors: {errors}"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while removing user from role. {ex}", ex);
                return ServiceResult<string>.Failure(new ServerErrorException("An error occurred while removing user from role. Please try again later."));
            }
        }


        private async Task<ServiceResult<User>> AuthenticateByUserNameOrPhone([Required][OneRequiredProp("UserName", "PhoneNumber")] LoginUserNameOrPhoneDto userLogin)
        {
            var user = new User();

            if (!string.IsNullOrEmpty(userLogin.UserName))
            {
                user = await _userManager.FindByNameAsync(userLogin.UserName);
                if (user == null)
                {
                    return ServiceResult<User>.Failure(
                        new BadRequestException("Invalid credentials. Please try again."));
                }
            }

            if (!string.IsNullOrEmpty(userLogin.PhoneNumber))
            {
                var phoneNumber = _securityUtilityService.NormalizePhoneNumber(userLogin.PhoneNumber);

                //First get all users in memory to check their phone numbers correctly
                var users = await _context.Users.ToListAsync();
                user = users.FirstOrDefault(x => _securityUtilityService.NormalizePhoneNumber(x.PhoneNumber ?? "") == phoneNumber);

                if (user == null)
                {
                    return ServiceResult<User>.Failure(
                        new BadRequestException("Invalid credentials. Please try again."));
                }
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);

            if (!result.Succeeded)
            {
                await Task.Delay(500); // slight delay for timing attack mitigation
                return ServiceResult<User>.Failure(
                    new BadRequestException("Invalid credentials. Please try again."));
            }

            return ServiceResult<User>.Success(user);
        }

        private async Task<User> Authenticate(UserLogin userLogin)
        {
            var user = await _userManager.FindByEmailAsync(userLogin.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(userLogin.Email);
                if (user == null && _securityUtilityService.NormalizePhoneNumber(userLogin.Email).Length > 5)
                {
                    user = _context.Users
                        .FirstOrDefault(x => _securityUtilityService.NormalizePhoneNumber(x.PhoneNumber) == _securityUtilityService.NormalizePhoneNumber(userLogin.Email));
                }
            }


            if (user != null)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);

                if (result.Succeeded)
                {
                    return user;
                }

            }
            return null;
        }


        //Save image upload file
        private async Task<ResultObject> SaveFile(IFormFile file)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;
            var obj = new ResultObject();

            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ApplicationException("No file was provided or file is empty");
                }

                // Use FileValidationService to validate file instead of duplicating validation logic
                var validationResult = await _fileValidationService.ValidateFileAsync(file);
                if (!validationResult.IsValid)
                {
                    throw new ApplicationException(validationResult.ErrorMessage);
                }

                // Get storage path from configuration
                string storagePath = _config.GetValue<string>("FileUploads:StoragePath", "media/images");

                // Fix path construction with Path.Combine
                string uploadPath = Path.Combine(webRootPath, storagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                // Ensure directory exists
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Get safe filename with extension
                string extension = Path.GetExtension(file.FileName);
                string fileNewName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadPath, fileNewName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Generate web-friendly URL path for the file
                string link = storagePath.TrimEnd('/') + "/" + fileNewName;
                link = link.Replace('\\', '/'); // Ensure forward slashes for URLs

                // Log successful upload
                _logger.LogInformation("File uploaded successfully: {FileName}, stored at {FilePath}", file.FileName, filePath);

                string msg = "Upload successful";
                obj.ReturnCode = "200";
                obj.ReturnDescription = msg;
                obj.Response = "Success";
                obj.Message = msg;
                obj.Link = link;
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError(ex, "File upload failed for file: {FileName}", file?.FileName);

                string msg = "An error occurred while attempting to upload file: " + ex.Message;
                obj.ReturnCode = "501";
                obj.ReturnDescription = msg;
                obj.Response = "Failed";
                obj.Message = msg;
            }

            return obj;
        }

        public async Task<ServiceResult<LoginResponseDto>> RefreshToken(string accessToken, string refreshToken)
        {
            var result = await _tokenService.RefreshToken(accessToken, refreshToken);
            if (result.IsSuccess)
            {
                var userId = _tokenService.GetUserIdFromToken(accessToken); // Assuming TokenService has this helper
                await _securityUtilityService.LogSecurityEvent(
                    userId,
                    "TokenRefresh",
                    "Access token refreshed",
                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                );
            }
            return result;
        }
        public async Task<ServiceResult<bool>> RevokeToken(string refreshToken)
        {
            var storedToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (storedToken == null)
                return ServiceResult<bool>.Failure(new NotFoundException("Refresh token not found"));

            var result = await _tokenService.RevokeRefreshToken(storedToken.UserId);
            if (result.IsSuccess)
            {
                await _securityUtilityService.LogSecurityEvent(
                    storedToken.UserId,
                    "TokenRevoke",
                    "Refresh token revoked",
                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                );
            }
            return result;
        }
        public async Task<ServiceResult<bool>> LogSecurityEvent(string userId, string eventType, string description, string ipAddress)
        {
            await _securityUtilityService.LogSecurityEvent(userId, eventType, description, ipAddress);
            return ServiceResult<bool>.Success(true);
        }
        private async Task SendLoginNotification(User user)
        {
            var emailDto = new EmailDto(true)
            {
                Subject = "A new Login has been detected",
                ToEmail = user.Email,
                Body = $"A new login into your account was detected at {DateTime.UtcNow}UTC. " +
                       $"If this wasn't you, please secure your account with {_config["ApplicationInfo:Name"]}  by changing your password. " +
                       $"Contact support if needed.<br/><br/>{_config["ApplicationInfo:SupportUrl"]}"
            };
            _emailSmtpService.SendMail(emailDto);
            //Task.Run(() => _emailSmtpController.SendMail(emailDto));
            await Task.CompletedTask;
        }
        public class ResultObject
        {
            public string ReturnCode { get; set; }
            public string ReturnDescription { get; set; }
            public string Response { get; set; }
            public string Message { get; set; }
            public string Link { get; set; }
            public string videoThumbnail { get; set; }
        }
    }
}
