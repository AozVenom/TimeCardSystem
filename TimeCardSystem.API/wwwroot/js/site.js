document.addEventListener('DOMContentLoaded', function () {
    // Find all logout links
    const logoutLinks = document.querySelectorAll('a[href="/Account/Logout"]');

    // Add click event handler to all logout links
    logoutLinks.forEach(link => {
        link.addEventListener('click', function (event) {
            // Prevent the default action
            event.preventDefault();

            // Ask for confirmation
            if (confirm('Are you sure you want to log out?')) {
                // If confirmed, redirect to the logout page
                window.location.href = '/Account/Logout';
            }
        });
    });
});