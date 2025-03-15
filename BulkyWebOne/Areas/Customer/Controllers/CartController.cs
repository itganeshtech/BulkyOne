using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWebOne.Areas.Customer.Controllers
{
      
        [Area("customer")]
        [Authorize]
        public class CartController : Controller
        {

            private readonly IUnitOfWork _unitOfWork;

        [BindProperty]    
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public object ShoopingCartVM { get; private set; }

        public CartController(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }


            public IActionResult Index()
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }

                return View(ShoppingCartVM);
            }


            public IActionResult Plus(int cartId)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            public IActionResult Minus(int cartId)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                if (cartFromDb.Count <= 1)
                {
                    //remove that from cart
                    _unitOfWork.ShoppingCart.Remove(cartFromDb);
                }
                else
                {
                    cartFromDb.Count -= 1;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            public IActionResult Remove(int cartId)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                 includeProperties: "Product"),
                OrderHeader = new()
            };


            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
            includeProperties: "Product");
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            if (ShoppingCartVM.OrderHeader.ApplicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Regular cutomer so payment method should be applied.
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //Company customer so payment method should not be applied.
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();


            foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }  
            return View(ShoppingCartVM);
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View();
        }



        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
            {
                if (shoppingCart.Count <= 50)
                {
                    return shoppingCart.Product.Price;
                }
                else
                {
                    if (shoppingCart.Count <= 100)
                    {
                        return shoppingCart.Product.Price50;
                    }
                    else
                    {
                        return shoppingCart.Product.Price100;
                    }
                }
            }
        

    }//End Class    
}//End Controller
