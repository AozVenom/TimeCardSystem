/**
 * Schedule management functionality for the Time Card System
 */
document.addEventListener('DOMContentLoaded', function () {
    // Elements for the create form
    const startDateInput = document.getElementById('shiftStartInput');
    const endDateInput = document.getElementById('shiftEndInput');
    const lunchOptionSelect = document.getElementById('lunchOptionSelect');

    // Elements for the edit form
    const editStartDateInput = document.getElementById('editShiftStartInput');
    const editEndDateInput = document.getElementById('editShiftEndInput');
    const editLunchOptionSelect = document.getElementById('editLunchOptionSelect');

    // Initialize with default values for create form
    if (startDateInput && !startDateInput.value) {
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        tomorrow.setHours(9, 0, 0, 0);
        startDateInput.value = formatDateForInput(tomorrow);

        // Calculate default end time
        calculateEndTime(startDateInput, endDateInput, lunchOptionSelect);
    }

    // Add event listeners for create form
    if (startDateInput && lunchOptionSelect) {
        startDateInput.addEventListener('change', function () {
            calculateEndTime(startDateInput, endDateInput, lunchOptionSelect);
        });

        lunchOptionSelect.addEventListener('change', function () {
            calculateEndTime(startDateInput, endDateInput, lunchOptionSelect);
        });
    }

    // Add event listeners for edit form
    if (editStartDateInput && editLunchOptionSelect) {
        editStartDateInput.addEventListener('change', function () {
            calculateEndTime(editStartDateInput, editEndDateInput, editLunchOptionSelect);
        });

        editLunchOptionSelect.addEventListener('change', function () {
            calculateEndTime(editStartDateInput, editEndDateInput, editLunchOptionSelect);
        });
    }

    // Function to calculate end time
    function calculateEndTime(startInput, endInput, lunchSelect) {
        if (!startInput.value) return;

        const startTime = new Date(startInput.value);
        const lunchOption = lunchSelect.value;

        // Calculate 8-hour shift plus lunch
        const endTime = new Date(startTime);

        // Add 8 hours for work
        endTime.setHours(endTime.getHours() + 8);

        // Add lunch duration if applicable
        if (lunchOption === "30") {
            endTime.setMinutes(endTime.getMinutes() + 30);
        } else if (lunchOption === "60") {
            endTime.setHours(endTime.getHours() + 1);
        }

        endInput.value = formatDateForInput(endTime);
    }

    // Function to format date for datetime-local input
    function formatDateForInput(date) {
        return date.getFullYear() + '-' +
            String(date.getMonth() + 1).padStart(2, '0') + '-' +
            String(date.getDate()).padStart(2, '0') + 'T' +
            String(date.getHours()).padStart(2, '0') + ':' +
            String(date.getMinutes()).padStart(2, '0');
    }

    // Form validation
    const createForm = document.querySelector('.schedule-form');
    if (createForm) {
        createForm.addEventListener('submit', function (e) {
            validateScheduleForm(e, startDateInput, endDateInput);
        });
    }

    const editForm = document.querySelector('.edit-schedule-form');
    if (editForm) {
        editForm.addEventListener('submit', function (e) {
            validateScheduleForm(e, editStartDateInput, editEndDateInput);
        });
    }

    function validateScheduleForm(e, startInput, endInput) {
        const startTime = new Date(startInput.value);
        const endTime = new Date(endInput.value);

        if (endTime <= startTime) {
            e.preventDefault();
            alert('Shift end time must be after shift start time');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        // Get the employee dropdown
        const employeeSelect = document.getElementById('NewSchedule_UserId');

        if (employeeSelect) {
            employeeSelect.addEventListener('change', function () {
                const selectedEmployeeId = this.value;
                if (selectedEmployeeId) {
                    // Fetch the employee's schedules using your existing API endpoint
                    fetch(`/api/schedule/user/${selectedEmployeeId}`)
                        .then(response => {
                            if (!response.ok) {
                                throw new Error('Error fetching schedules');
                            }
                            return response.json();
                        })
                        .then(data => {
                            // Update UI to show the schedules
                            displayEmployeeSchedules(data);
                        })
                        .catch(error => console.error('Error:', error));
                } else {
                    // Clear the display if no employee is selected
                    const container = document.getElementById('current-employee-schedules');
                    if (container) {
                        container.innerHTML = '';
                    }
                }
            });
        }

        function displayEmployeeSchedules(schedules) {
            // Create or find the container for employee schedules
            let container = document.getElementById('current-employee-schedules');
            if (!container) {
                container = document.createElement('div');
                container.id = 'current-employee-schedules';
                container.className = 'employee-schedules-container mt-4';

                // Insert after the form
                const formContainer = document.querySelector('.schedule-form-container');
                if (formContainer) {
                    formContainer.parentNode.insertBefore(container, formContainer.nextSibling);
                } else {
                    // Fallback: append to the main content area
                    document.querySelector('.main-content').appendChild(container);
                }
            }

            // Clear existing content
            container.innerHTML = '';

            // Display header
            const header = document.createElement('h4');
            header.textContent = 'Current Schedules for Selected Employee';
            container.appendChild(header);

            // Display schedules
            if (!schedules || schedules.length === 0) {
                container.innerHTML += '<p>No existing schedules found for this employee.</p>';
            } else {
                const table = document.createElement('table');
                table.className = 'table table-striped';
                table.innerHTML = `
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Time</th>
                        <th>Location</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
                    ${schedules.map(s => `
                        <tr>
                            <td>${new Date(s.shiftStart).toLocaleDateString()}</td>
                            <td>${new Date(s.shiftStart).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - 
                                ${new Date(s.shiftEnd).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</td>
                            <td>${s.location || ''}</td>
                            <td><span class="status-badge status-${s.status.toLowerCase()}">${s.status}</span></td>
                        </tr>
                    `).join('')}
                </tbody>
            `;
                container.appendChild(table);
            }
        }
    });
});
function checkForConflicts() {
    const employeeId = document.getElementById('NewSchedule_UserId').value;
    const shiftStart = document.getElementById('shiftStartInput').value;
    const shiftEnd = document.getElementById('shiftEndInput').value;

    // Clear previous warnings
    const warningElement = document.getElementById('schedule-conflict-warning');
    if (warningElement) {
        warningElement.remove();
    }

    if (!employeeId || !shiftStart || !shiftEnd) {
        return; // Not enough info to check
    }

    // Format the dates for the API
    const formattedStart = new Date(shiftStart).toISOString();
    const formattedEnd = new Date(shiftEnd).toISOString();

    // Check for conflicts using the API
    fetch(`/api/schedule/check-conflicts?userId=${employeeId}&shiftStart=${formattedStart}&shiftEnd=${formattedEnd}`)
        .then(response => response.json())
        .then(data => {
            if (data.hasConflict) {
                // Create and show warning
                const warning = document.createElement('div');
                warning.id = 'schedule-conflict-warning';
                warning.className = 'alert alert-warning mt-2';
                warning.innerHTML = '<strong>Warning:</strong> This schedule conflicts with an existing schedule for this employee.';

                // Insert after the shift end input
                const shiftEndGroup = document.getElementById('shiftEndInput').closest('.form-group');
                shiftEndGroup.parentNode.insertBefore(warning, shiftEndGroup.nextSibling);
            }
        })
        .catch(error => console.error('Error checking conflicts:', error));
}

// Add event listeners to trigger conflict check
document.addEventListener('DOMContentLoaded', function () {
    const employeeSelect = document.getElementById('NewSchedule_UserId');
    const shiftStartInput = document.getElementById('shiftStartInput');
    const shiftEndInput = document.getElementById('shiftEndInput');

    // Attach conflict checking to relevant fields
    if (employeeSelect && shiftStartInput && shiftEndInput) {
        employeeSelect.addEventListener('change', checkForConflicts);
        shiftStartInput.addEventListener('change', checkForConflicts);
        shiftEndInput.addEventListener('change', checkForConflicts);
    }
});