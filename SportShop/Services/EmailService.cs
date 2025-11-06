using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SportShop.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appPassword;
        private readonly string _smtpHost;
        private readonly int _smtpPort;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _fromEmail = _configuration["Email:FromEmail"] ?? "your-email@gmail.com";
            _fromName = _configuration["Email:FromName"] ?? "SportShop";
            _appPassword = _configuration["Email:AppPassword"] ?? "";
            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        }

        /// <summary>
        /// Gửi email OTP xác thực đăng ký
        /// </summary>
        public async Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otpCode)
        {
            try
            {
                var subject = "Mã xác thực đăng ký tài khoản SportShop";
                var body = GenerateOtpEmailBody(toName, otpCode);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gửi email chào mừng sau khi đăng ký thành công
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "Chào mừng đến với SportShop!";
                var body = GenerateWelcomeEmailBody(userName);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending welcome email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gửi email OTP để đặt lại mật khẩu
        /// </summary>
        public async Task<bool> SendResetPasswordOtpAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "Mã OTP đặt lại mật khẩu - SportShop";
                var body = GenerateResetPasswordOtpEmailBody(userName, otpCode);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reset password OTP: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gửi email chung
        /// </summary>
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_fromEmail, _appPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, _fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tạo nội dung email OTP
        /// </summary>
        private string GenerateOtpEmailBody(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .otp-box {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            margin: 30px 0;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: bold;
            letter-spacing: 8px;
            margin: 10px 0;
        }}
        .info-box {{
            background: #f8f9fa;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin: 20px 0;
            border-radius: 5px;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 14px;
        }}
        .warning {{
            color: #dc3545;
            font-weight: bold;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏃 SportShop</h1>
            <p>Xác thực đăng ký tài khoản</p>
        </div>
        
        <div class='content'>
            <h2>Xin chào {userName}!</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại SportShop. Để hoàn tất quá trình đăng ký, vui lòng sử dụng mã OTP dưới đây:</p>
            
            <div class='otp-box'>
                <p style='margin: 0; font-size: 14px;'>Mã xác thực của bạn</p>
                <div class='otp-code'>{otpCode}</div>
                <p style='margin: 0; font-size: 14px;'>Có hiệu lực trong 5 phút</p>
            </div>
            
            <div class='info-box'>
                <p style='margin: 0;'><strong>⏰ Lưu ý:</strong></p>
                <ul style='margin: 10px 0;'>
                    <li>Mã OTP có hiệu lực trong <strong>5 phút</strong></li>
                    <li>Không chia sẻ mã này với bất kỳ ai</li>
                    <li>Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này</li>
                </ul>
            </div>
            
            <p class='warning'>⚠️ Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi ngay.</p>
        </div>
        
        <div class='footer'>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p>&copy; 2025 SportShop. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Tạo nội dung email chào mừng
        /// </summary>
        private string GenerateWelcomeEmailBody(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 20px;
            text-align: center;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .btn {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng đến với SportShop!</h1>
        </div>
        
        <div class='content'>
            <h2>Xin chào {userName}!</h2>
            <p>Chúc mừng bạn đã đăng ký tài khoản thành công tại <strong>SportShop</strong>!</p>
            <p>Tài khoản của bạn đã được kích hoạt và sẵn sàng sử dụng.</p>
            
            <p>Tại SportShop, bạn có thể:</p>
            <ul>
                <li>✓ Mua sắm hàng nghìn sản phẩm thể thao chất lượng</li>
                <li>✓ Theo dõi đơn hàng dễ dàng</li>
                <li>✓ Nhận thông báo khuyến mãi đặc biệt</li>
                <li>✓ Tích điểm và nhận ưu đãi</li>
            </ul>
            
            <div style='text-align: center;'>
                <a href='http://localhost:5084' class='btn'>Bắt đầu mua sắm</a>
            </div>
        </div>
        
        <div class='footer'>
            <p>Cảm ơn bạn đã tin tưởng SportShop!</p>
            <p>&copy; 2025 SportShop. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Tạo nội dung email OTP đặt lại mật khẩu
        /// </summary>
        private string GenerateResetPasswordOtpEmailBody(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
            text-align: center;
            color: white;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .otp-box {{
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            border-radius: 10px;
            padding: 30px;
            text-align: center;
            margin: 30px 0;
            border: 2px dashed #667eea;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 8px;
            margin: 10px 0;
        }}
        .warning {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning p {{
            margin: 5px 0;
            color: #856404;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 14px;
            color: #666;
        }}
        .icon {{
            font-size: 48px;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>🔐</div>
            <h1>Đặt lại mật khẩu</h1>
        </div>
        
        <div class='content'>
            <h2>Xin chào {userName}!</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản SportShop của bạn.</p>
            
            <div class='otp-box'>
                <p style='margin: 0; font-size: 16px; color: #666;'>Mã OTP của bạn là:</p>
                <div class='otp-code'>{otpCode}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px; color: #999;'>Mã có hiệu lực trong 5 phút</p>
            </div>
            
            <p>Vui lòng nhập mã OTP này để tiếp tục quá trình đặt lại mật khẩu.</p>
            
            <div class='warning'>
                <p><strong>⚠️ Lưu ý bảo mật:</strong></p>
                <p>• Không chia sẻ mã OTP này với bất kỳ ai</p>
                <p>• Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</p>
                <p>• Mã OTP sẽ hết hạn sau 5 phút</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='http://localhost:5084/Account/VerifyResetOtp' class='btn'>Xác thực ngay</a>
            </div>
        </div>
        
        <div class='footer'>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p>&copy; 2025 SportShop. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Tạo mã OTP ngẫu nhiên 6 chữ số
        /// </summary>
        public static string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
