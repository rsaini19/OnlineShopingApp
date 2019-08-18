using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OnlineShopingApp.Models;
using System.Net.Http;
using System.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Dynamic;
using System.IO;
using System.Drawing;
using OnlineShopingApp.Master_Caching;
using OnlineShopingApp.Filters;
using System.Threading;

namespace OnlineShopingApp.Controllers
{
    [NoCache]
    public class ProductController : Controller
    {
        HttpClient client = null;
        string BaseUrl = string.Empty;
        string apiUrl = string.Empty;
        Dictionary<int, string> getCategory = null;

        public ProductController()
        {
            client = (HttpClient)System.Web.HttpContext.Current.Session["ApiSetting"];
            BaseUrl = client.BaseAddress.AbsoluteUri + "Product/";
            getCategory = CategoryCaching.GetCategoryById();
        }

        // GET: Product
        public ActionResult Index()
        {
            List<ProductImageViewModel> ProductImageList = new List<ProductImageViewModel>();
            List<ProductViewModel> ProductList = new List<ProductViewModel>();

            apiUrl = BaseUrl + "GetAll/" + 1;
            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {

                var responseData = result.Content.ReadAsStringAsync().Result;
                ProductList = JsonConvert.DeserializeObject<List<ProductViewModel>>(responseData);

                if (ProductList.Count > 0)
                {
                    int TotalCount = ProductList[0].TotalCount;

                    if (TotalCount > 10)
                    {
                        int taskCount = (TotalCount % 10) == 0 ? TotalCount / 10 : (TotalCount / 10) + 1;

                        Task<List<ProductViewModel>>[] taskArray = new Task<List<ProductViewModel>>[taskCount - 1];

                        for (int i = 0; i < taskArray.Length; i++)
                        {
                                taskArray[i] = Task.Factory.StartNew(() => GetParallelProducts(i + 2));
                                Thread.Sleep(100);
                        }


                        Task.WaitAll(taskArray);
                        for (int i = 0; i < taskArray.Length; i++)
                            ProductList.AddRange(taskArray[i].Result);
                    }

                }

                //foreach (var product in ProductList)
                //{
                //    product.CategoryName = getCategory[product.CategoryId].ToString();
                //}
            }

            return View(ProductList);
        }

        // GET: Product/Details/5
        public ActionResult Details(int id)
        {
            ProductViewModel Product = new ProductViewModel();
            apiUrl = apiUrl + "Get/" + id;

            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {
                var responseData = result.Content.ReadAsStringAsync().Result;
                Product = JsonConvert.DeserializeObject<ProductViewModel>(responseData);
            }
            if (Product != null)
                Product.CategoryName = getCategory[Product.CategoryId].ToString();
            else
                return HttpNotFound();
            Dispose(true);
            return View(Product);
        }

        // GET: Product/Create
        public ActionResult Create()
        {
            ProductImageViewModel productDetails = new ProductImageViewModel();
            productDetails.Product = new ProductViewModel();
            var Categories = CategoryCaching.GetAllCategory();
            TempData["Categories"] = new SelectList(Categories, "CategoryId", "CategoryName");
            TempData.Keep();
            return View(productDetails);
        }

        // POST: Product/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductImageViewModel productimageModel)
        {
            byte[] imageData = null;
            ProductViewModel productModel = new ProductViewModel();
            apiUrl = apiUrl + "Add";

            if (productimageModel.PostedFile != null)
                imageData = FiletoByteArray(productimageModel);

            if (productimageModel.Product != null)
            {
                productModel.ProductName = productimageModel.Product.ProductName;
                productModel.ProductDesc = productimageModel.Product.ProductDesc;
                productModel.ProductPrice = productimageModel.Product.ProductPrice;
                productModel.ProductImage = imageData;
                productModel.CategoryId = productimageModel.Product.CategoryId;

                var stringContent = new StringContent(JsonConvert.SerializeObject(productModel), Encoding.UTF8, "application/json");

                var postTask = client.PostAsync(apiUrl, stringContent);
                postTask.Wait();

                var result = postTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "Server Error. Please contact administrator.");
                Dispose(true);
            }

            return View(productimageModel);
        }

        // GET: Product/Edit/5
        public ActionResult Edit(int? id)
        {
            ProductImageViewModel ProductDetails = new ProductImageViewModel();
            apiUrl = apiUrl + "Get/" + id;

            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {
                var responseData = result.Content.ReadAsStringAsync().Result;
                ProductDetails.Product = JsonConvert.DeserializeObject<ProductViewModel>(responseData);
            }
            if (ProductDetails != null)
            {
                var Categories = CategoryCaching.GetAllCategory();
                TempData["Categories"] = new SelectList(Categories, "CategoryId", "CategoryName");
                TempData.Keep();
                ProductDetails.Product.CategoryName = getCategory[ProductDetails.Product.CategoryId].ToString();

                TempData["image"] = ProductDetails.Product.ProductImage;
                TempData.Keep();
            }
            else
                return HttpNotFound();
            Dispose(true);
            return View(ProductDetails);
        }

        // POST: Product/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductImageViewModel productimageModel)
        {
            byte[] imageData = null;
            ProductViewModel productModel = new ProductViewModel();
            apiUrl = apiUrl + "Update";

            if (productimageModel.PostedFile != null)
                imageData = FiletoByteArray(productimageModel);
            else
                imageData = (byte[])TempData["image"];

            if (productimageModel.Product != null)
            {
                productModel.ProductId = productimageModel.Product.ProductId;
                productModel.ProductName = productimageModel.Product.ProductName;
                productModel.ProductDesc = productimageModel.Product.ProductDesc;
                productModel.ProductPrice = productimageModel.Product.ProductPrice;
                productModel.ProductImage = imageData;
                productModel.CategoryId = productimageModel.Product.CategoryId;

                var stringContent = new StringContent(JsonConvert.SerializeObject(productModel), Encoding.UTF8, "application/json");

                var postTask = client.PostAsync(apiUrl, stringContent);
                postTask.Wait();

                var result = postTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "Server Error. Please contact administrator.");
            }
            Dispose(true);
            return View(productimageModel);
        }

        // GET: Product/Delete/5
        public ActionResult Delete(int? id)
        {
            ProductViewModel Product = new ProductViewModel();
            apiUrl = apiUrl + "Get/" + id;

            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {
                var responseData = result.Content.ReadAsStringAsync().Result;
                Product = JsonConvert.DeserializeObject<ProductViewModel>(responseData);
            }
            if (Product != null)
                Product.CategoryName = getCategory[Product.CategoryId].ToString();
            else
                return HttpNotFound();
            Dispose(true);
            return View(Product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int? id)
        {
            apiUrl = apiUrl + "Delete/" + id;
            var stringContent = new StringContent(id.ToString(), Encoding.UTF8, "application/json");

            var deleteTask = client.PostAsync(apiUrl, stringContent);
            deleteTask.Wait();
            var result = deleteTask.Result;

            Dispose(true);
            return RedirectToAction("Index");
        }

        public ActionResult ProductCategory(int id)
        {
            apiUrl = apiUrl + "GetProductsbyCategory/" + id;
            List<ProductViewModel> lstProduct = new List<ProductViewModel>();

            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {
                var responseData = result.Content.ReadAsStringAsync().Result;
                lstProduct = JsonConvert.DeserializeObject<List<ProductViewModel>>(responseData);
            }

            string CategoryName = getCategory[id].ToString();

            lstProduct.ForEach(p => p.CategoryName = CategoryName);

            Dispose(true);
            //Session["ReturnUrl"] = ControllerContext.HttpContext.Request.UrlReferrer.ToString();
            return View("ProductsbyCategory", lstProduct);
        }

        //[Route("Product/ProductsbyCategory/AddtoCart")]
        public ActionResult AddtoCart(string Quantity, string ProductId, string ProductName, string ProductDesc, string productPrice)
        {
            if (Request.Cookies["cart"] == null)
            {
                HttpCookie cartCookie = new HttpCookie("cart");
                for (int i = 0; i < Convert.ToInt32(Quantity); i++)
                {
                    if (i == 0)
                        cartCookie.Value = (i + 1) + "," + ProductId + "," + ProductName + "," + ProductDesc + "," + productPrice;
                    else
                        cartCookie.Value = cartCookie.Value + "|" + (i + 1) + "," + ProductId + "," + ProductName + "," + ProductDesc + "," + productPrice;
                }
                cartCookie.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(cartCookie);
            }
            else
            {
                int j = Request.Cookies["cart"].Value.Split('|').Length;
                for (int i = 0; i < Convert.ToInt32(Quantity); i++)
                    Request.Cookies["cart"].Value = Request.Cookies["cart"].Value + "|" + (j + (i + 1)) + "," + ProductId + "," + ProductName + "," + ProductDesc + "," + productPrice;

                Request.Cookies["cart"].Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(Request.Cookies["cart"]);
            }
            return null;
        }

        public ActionResult ViewCart()
        {
            List<CartViewModel> lstCartDetails = new List<CartViewModel>();

            if (Request.Cookies["cart"] != null && !(string.IsNullOrEmpty(Request.Cookies["cart"].Value)))
            {
                string[] Products = Request.Cookies["cart"].Value.Split('|');
                if (Products != null && Products.Length > 0)
                {
                    int count = 0;
                    foreach (var product in Products)
                    {
                        string[] items = product.Split(',');
                        if (items != null && items.Length > 0)
                        {
                            CartViewModel productDetails = new CartViewModel();
                            productDetails.CartId = Convert.ToInt32(items[0]);
                            productDetails.ProductId = Convert.ToInt32(items[1]);
                            productDetails.ProductName = Convert.ToString(items[2]);
                            productDetails.ProductDesc = Convert.ToString(items[3]);
                            productDetails.ProductPrice = Convert.ToDouble(items[4]);
                            lstCartDetails.Add(productDetails);
                        }
                        count++;
                    }
                }
            }
            return View(lstCartDetails);
        }

        public ActionResult DeleteCart(string Cart)
        {
            string carts = Cart.TrimStart(',').TrimEnd(',').Replace(",,", ",");
            string[] CartIds = carts.Split(',');

            DataTable CartTable = new DataTable();
            CartTable.Clear();
            CartTable.Columns.Add("CartId", typeof(string));
            CartTable.Columns.Add("ProductId", typeof(string));
            CartTable.Columns.Add("ProductName", typeof(string));
            CartTable.Columns.Add("ProductDesc", typeof(string));
            CartTable.Columns.Add("productPrice", typeof(string));

            if (Request.Cookies["cart"] != null)
            {
                string[] Products = Request.Cookies["cart"].Value.Split('|');
                if (Products != null && Products.Length > 0)
                {
                    foreach (var product in Products)
                    {
                        DataRow drCart = CartTable.NewRow();
                        string[] items = product.Split(',');
                        if (items != null && items.Length > 0)
                        {
                            drCart["CartId"] = Convert.ToInt32(items[0]);
                            drCart["ProductId"] = Convert.ToInt32(items[1]);
                            drCart["ProductName"] = Convert.ToString(items[2]);
                            drCart["ProductDesc"] = Convert.ToString(items[3]);
                            drCart["ProductPrice"] = Convert.ToDouble(items[4]);
                        }
                        CartTable.Rows.Add(drCart);
                    }
                }
            }

            Request.Cookies["cart"].Expires = DateTime.Now.AddDays(-1);
            Response.Cookies.Add(Request.Cookies["cart"]);

            foreach (var cartId in CartIds)
                CartTable.AsEnumerable().Where(r => r.Field<string>("CartId") == cartId).ToList().ForEach(row => row.Delete());

            HttpCookie cartCookie = new HttpCookie("cart");
            if (CartTable.Rows.Count == 0)
            {
                cartCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cartCookie);
            }
            else
            {
                for (int i = 0; i < CartTable.Rows.Count; i++)
                {
                    if (i == 0)
                        cartCookie.Value = (i + 1) + "," + CartTable.Rows[i].ItemArray[1].ToString() + "," + CartTable.Rows[i].ItemArray[2].ToString() + "," + CartTable.Rows[i].ItemArray[3].ToString() + "," + CartTable.Rows[i].ItemArray[4].ToString();
                    else
                        cartCookie.Value = cartCookie.Value + "|" + (i + 1) + "," + CartTable.Rows[i].ItemArray[1].ToString() + "," + CartTable.Rows[i].ItemArray[2].ToString() + "," + CartTable.Rows[i].ItemArray[3].ToString() + "," + CartTable.Rows[i].ItemArray[4].ToString();
                }
                cartCookie.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(cartCookie);
            }

            return RedirectToAction("ViewCart");

        }

        public JsonResult GetProductAutoSearch(string prefix)
        {
            apiUrl = apiUrl + "GetProductAutoSearch/" + prefix;

            List<ProductAutoSearch> lstProductSearch = new List<ProductAutoSearch>();

            var GetTask = client.GetAsync(apiUrl);
            GetTask.Wait();

            var result = GetTask.Result;

            if (result.IsSuccessStatusCode)
            {
                string responseData = result.Content.ReadAsStringAsync().Result;
                lstProductSearch = JsonConvert.DeserializeObject<List<ProductAutoSearch>>(responseData);
            }
            return Json(lstProductSearch, JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                client = null;
            }
            base.Dispose(disposing);
        }

        protected byte[] FiletoByteArray(ProductImageViewModel productimageModel)
        {
            using (Stream inputStream = productimageModel.PostedFile.InputStream)
            {
                MemoryStream memoryStream = inputStream as MemoryStream;
                if (memoryStream == null)
                {
                    memoryStream = new MemoryStream();
                    inputStream.CopyTo(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        private List<ProductViewModel> GetParallelProducts(int index)
        {
            Object obj = new Object();
            lock (obj)
            {
                List<ProductViewModel> Products = null;
                apiUrl = string.Empty;
                apiUrl = BaseUrl + "GetAll/" + index;
                var Getproduct = client.GetAsync(apiUrl);
                Getproduct.Wait();
                var Productresult = Getproduct.Result;

                if (Productresult.IsSuccessStatusCode)
                {
                    var response = Productresult.Content.ReadAsStringAsync().Result;
                    Products = JsonConvert.DeserializeObject<List<ProductViewModel>>(response);
                }
                return Products;
            }
            
        }

    }
}
