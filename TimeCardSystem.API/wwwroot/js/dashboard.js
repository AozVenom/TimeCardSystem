// Wait for DOM to be fully loaded before running any code
document.addEventListener('DOMContentLoaded', function () {
    console.log("Dashboard.js loaded");

    // Current week start date (global variable)
    let currentWeekStart = new Date();
    // Set to the beginning of the current week (Monday for your existing interface)
    const today = new Date();
    const currentDayOfWeek = today.getDay(); // 0 = Sunday, 1 = Monday, etc.
    currentWeekStart = new Date(today);
    currentWeekStart.setDate(today.getDate() - (currentDayOfWeek === 0 ? 6 : currentDayOfWeek - 1));
    currentWeekStart.setHours(0, 0, 0, 0);

    // Digital clock functionality
    function updateClock() {
        const now = new Date();

        // Update time (in 12-hour format with AM/PM)
        let hours = now.getHours();
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';

        // Convert 24-hour to 12-hour format
        hours = hours % 12;
        hours = hours ? hours : 12; // Convert 0 to 12 for midnight
        hours = String(hours).padStart(2, '0');

        // Get all clock elements
        const clockTimeElements = document.querySelectorAll('#clock-time');
        const clockAmPmElements = document.querySelectorAll('#clock-ampm');
        const clockDateElements = document.querySelectorAll('#clock-date');

        // Update all clock time elements
        clockTimeElements.forEach(element => {
            element.textContent = `${hours}:${minutes}`;
        });

        // Update all AM/PM indicators
        clockAmPmElements.forEach(element => {
            element.textContent = ampm;
        });

        // Update all date elements
        const options = { weekday: 'short', month: 'short', day: 'numeric' };
        const dateString = now.toLocaleDateString('en-US', options);
        clockDateElements.forEach(element => {
            element.textContent = dateString;
        });
    }

    // Initialize clock and update every second
    updateClock();
    setInterval(updateClock, 1000);

    // Format date for display (MMM D format)
    function formatDate(date) {
        const options = { month: 'short', day: 'numeric' };
        return date.toLocaleDateString('en-US', options);
    }

    // Format date for API (YYYY-MM-DD)
    function formatDateForApi(date) {
        return date.toISOString().split('T')[0];
    }

    // Check if the given date is in the current week
    function isCurrentWeek(date) {
        const today = new Date();
        const currentDayOfWeek = today.getDay(); // 0 = Sunday, 1 = Monday, etc.
        const mondayOfThisWeek = new Date(today);
        mondayOfThisWeek.setDate(today.getDate() - (currentDayOfWeek === 0 ? 6 : currentDayOfWeek - 1));
        mondayOfThisWeek.setHours(0, 0, 0, 0);

        return date.toDateString() === mondayOfThisWeek.toDateString();
    }

    // Function to format time (HH:MM AM/PM)
    function formatTime(dateTimeStr) {
        if (!dateTimeStr) return "N/A";

        const date = new Date(dateTimeStr);
        let hours = date.getHours();
        const minutes = date.getMinutes().toString().padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';

        hours = hours % 12;
        hours = hours ? hours : 12; // Convert 0 to 12 for 12 AM

        return `${hours}:${minutes} ${ampm}`;
    }

    // Function to get the anti-forgery token
    function getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : null;
    }

    // Expose the navigateWeek function to global scope so it can be accessed by onclick
    window.navigateWeek = function (direction) {
        const weekDisplay = document.getElementById('week-display');
        if (!weekDisplay) return;

        // Parse current week start date if not set
        if (!currentWeekStart) {
            try {
                // Extract dates from format like "This Week (Apr 1 - Apr 7)"
                const currentWeekText = weekDisplay.innerText;
                const dateRange = currentWeekText.match(/\((.*?)\)/)[1];
                const dates = dateRange.split(' - ');
                currentWeekStart = new Date(dates[0] + ', ' + new Date().getFullYear());
            } catch (e) {
                // Fallback if parsing fails
                const today = new Date();
                const currentDayOfWeek = today.getDay(); // 0 = Sunday, 1 = Monday, etc.
                currentWeekStart = new Date(today);
                currentWeekStart.setDate(today.getDate() - (currentDayOfWeek === 0 ? 6 : currentDayOfWeek - 1));
                currentWeekStart.setHours(0, 0, 0, 0);
            }
        }

        // Calculate new week start date
        const newWeekStart = new Date(currentWeekStart);
        newWeekStart.setDate(newWeekStart.getDate() + (direction * 7));

        // Calculate new week end date
        const newWeekEnd = new Date(newWeekStart);
        newWeekEnd.setDate(newWeekEnd.getDate() + 6);

        // Show loading state
        const originalWeekDisplayText = weekDisplay.innerText;
        weekDisplay.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';

        // Find hours summary if it exists
        const hoursSummary = document.querySelector('.hours-summary .hours-total .hours-value');
        const hoursTotalOriginal = hoursSummary ? hoursSummary.innerText : '';
        if (hoursSummary) {
            hoursSummary.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        }

        // Fetch data for the new week
        fetchWeeklyTimeData(newWeekStart).then(data => {
            // Update display after data is loaded
            const weekLabel = isCurrentWeek(newWeekStart) ? "This Week" : "Week of";
            weekDisplay.innerText = `${weekLabel} (${formatDate(newWeekStart)} - ${formatDate(newWeekEnd)})`;

            // Update current week start date for future navigation
            currentWeekStart = newWeekStart;

            // Update the hours summary
            if (hoursSummary) {
                hoursSummary.textContent = data.totalHours.toFixed(1);
            }
        }).catch(error => {
            console.error("Error fetching weekly data:", error);

            // Restore original text on error
            weekDisplay.innerText = originalWeekDisplayText;

            if (hoursSummary) {
                hoursSummary.textContent = hoursTotalOriginal;
            }

            // Show error notification
            alert("Failed to load weekly data. Please try again.");
        });
    };

    // Function to fetch weekly time data from the server
    async function fetchWeeklyTimeData(weekStart) {
        try {
            // Format date for the API request (YYYY-MM-DD)
            const formattedDate = formatDateForApi(weekStart);

            // Make API request to the server
            const response = await fetch(`/api/TimeEntry/weekly?weekStart=${formattedDate}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`Server responded with status: ${response.status}`);
            }

            const data = await response.json();

            // Update the UI with the fetched data
            updateWeeklyDisplay(data);

            return data;
        } catch (error) {
            console.error("Error fetching weekly time data:", error);
            throw error;
        }
    }

    // Function to update the UI with weekly time data
    function updateWeeklyDisplay(data) {
        // Update weekly total hours
        const hoursSummary = document.querySelector('.hours-summary .hours-total .hours-value');
        if (hoursSummary) {
            hoursSummary.textContent = data.totalHours.toFixed(1);
        }

        // Find all day cards
        const dayCards = document.querySelectorAll('.day-card');
        if (!dayCards.length) return;

        // Map day of week names to indices
        const dayNames = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

        // Update each day card with data
        data.dailyEntries.forEach(dayData => {
            // Get day of week (0-6, where 0 is Monday in our UI)
            const date = new Date(dayData.date);
            const dayOfWeek = date.getDay(); // 0=Sunday, 1=Monday, etc.
            const dayIndex = dayOfWeek === 0 ? 6 : dayOfWeek - 1; // Convert to 0=Monday, 6=Sunday

            // Find the card for this day
            const dayCard = Array.from(dayCards).find(card => {
                const dayNameElement = card.querySelector('.day-name');
                return dayNameElement && dayNameElement.textContent === dayNames[dayIndex];
            });

            if (!dayCard) return;

            // Update the hours
            const hoursElement = dayCard.querySelector('.day-hours');
            if (hoursElement) {
                hoursElement.textContent = dayData.totalHours.toFixed(1);
            }

            // Update the details
            const detailsElement = dayCard.querySelector('.day-details');
            if (detailsElement) {
                if (dayData.entries && dayData.entries.length > 0) {
                    // Show the first entry (or the active one if available)
                    const activeEntry = dayData.entries.find(e => e.clockOut === null);
                    const entry = activeEntry || dayData.entries[0];

                    const clockInTime = formatTime(entry.clockIn);
                    const clockOutTime = entry.clockOut ? formatTime(entry.clockOut) : "Active";

                    detailsElement.innerHTML = `<span>${clockInTime} - ${clockOutTime}</span>`;
                } else {
                    detailsElement.innerHTML = "<span>No time entry</span>";
                }
            }
        });
    }

    // Ensure the initial week display matches our currentWeekStart
    const weekDisplay = document.getElementById('week-display');
    if (weekDisplay) {
        const weekEnd = new Date(currentWeekStart);
        weekEnd.setDate(weekEnd.getDate() + 6);

        const weekLabel = isCurrentWeek(currentWeekStart) ? "This Week" : "Week of";
        weekDisplay.innerText = `${weekLabel} (${formatDate(currentWeekStart)} - ${formatDate(weekEnd)})`;
    }
});