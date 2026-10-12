// Guard inline — chạy ngay trước render
// Copy vào <head> của mọi trang cần bảo vệ
(function(){
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.replace('/index.html');
    }
})();
