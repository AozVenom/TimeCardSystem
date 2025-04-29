/**
 * Admin functionality for user management
 */
document.addEventListener('DOMContentLoaded', function () {
    // Get elements
    const createUserForm = document.getElementById('createUserForm');
    const editUserForm = document.getElementById('editUserForm');
    const resetPasswordCheckbox = document.getElementById('resetPassword');
    const searchInput = document.getElementById('searchUsers');
    const roleFilter = document.getElementById('roleFilter');
    const statusFilter = document.getElementById('statusFilter');
    const itemsPerPage = document.getElementById('itemsPerPage');
    const selectAllCheckbox = document.getElementById('selectAllUsers');
    const bulkActionSelect = document.getElementById('bulkActionSelect');
    const applyBulkActionBtn = document.getElementById('applyBulkAction');
    const exportBtn = document.getElementById('exportUsersBtn');

    // Bootstrap modal instances
    let editUserModal, createUserModal, deleteUserModal;
    let createConfirmModal, editConfirmModal, deleteConfirmModal, statusConfirmModal, bulkConfirmModal;

    // Initialize Bootstrap modals
    if (typeof bootstrap !== 'undefined') {
        // Action modals
        createUserModal = new bootstrap.Modal(document.getElementById('createUserModal'));
        editUserModal = new bootstrap.Modal(document.getElementById('editUserModal'));
        deleteUserModal = new bootstrap.Modal(document.getElementById('deleteUserModal'));

        // Confirmation modals
        createConfirmModal = new bootstrap.Modal(document.getElementById('createConfirmModal'));
        editConfirmModal = new bootstrap.Modal(document.getElementById('editConfirmModal'));
        deleteConfirmModal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        statusConfirmModal = new bootstrap.Modal(document.getElementById('statusConfirmModal'));
        bulkConfirmModal = new bootstrap.Modal(document.getElementById('bulkConfirmModal'));
    }

    /**
     * Show status message
     */
    function showStatusMessage(message, isError = false) {
        const container = document.getElementById('statusMessageContainer');
        const messageText = document.getElementById('statusMessageText');

        if (container && messageText) {
            container.classList.remove('d-none');
            container.querySelector('.alert').className = `alert ${isError ? 'alert-danger' : 'alert-success'} alert-dismissible fade show`;
            messageText.textContent = message;

            // Auto-hide after 5 seconds
            setTimeout(() => {
                container.classList.add('d-none');
            }, 5000);
        }
    }

    /**
     * Create User Form Submission
     */
    if (createUserForm) {
        createUserForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const userData = {
                firstName: document.getElementById('createFirstName').value,
                lastName: document.getElementById('createLastName').value,
                email: document.getElementById('createEmail').value,
                role: parseInt(document.getElementById('createRole').value),
                password: document.getElementById('createPassword').value
            };

            fetch('/api/UserAdmin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(userData)
            })
                .then(response => {
                    if (!response.ok) {
                        return response.text().then(text => { throw new Error(text) });
                    }
                    return response.json();
                })
                .then(data => {
                    // Hide create form and show success modal
                    createUserModal.hide();
                    createUserForm.reset();

                    // Show confirmation modal instead of immediate reload
                    createConfirmModal.show();
                })
                .catch(error => {
                    console.error('Error creating user:', error);
                    showStatusMessage(error.message || 'Failed to create user.', true);
                });
        });
    }

    // Create confirmation button click
    document.getElementById('createConfirmBtn')?.addEventListener('click', function () {
        createConfirmModal.hide();
        window.location.reload();
    });

    /**
     * Edit User Button Click
     */
    document.querySelectorAll('.btn-edit').forEach(button => {
        button.addEventListener('click', function () {
            const userId = this.getAttribute('data-user-id');

            fetch(`/api/UserAdmin/${userId}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Failed to get user data');
                    }
                    return response.json();
                })
                .then(user => {
                    document.getElementById('editUserId').value = user.id;
                    document.getElementById('editFirstName').value = user.firstName;
                    document.getElementById('editLastName').value = user.lastName;
                    document.getElementById('editEmail').value = user.email;
                    document.getElementById('editRole').value = user.role;

                    // Reset password fields
                    document.getElementById('resetPassword').checked = false;
                    document.querySelector('.password-reset-group').style.display = 'none';

                    // Show the modal
                    editUserModal.show();
                })
                .catch(error => {
                    console.error('Error fetching user:', error);
                    showStatusMessage('Failed to load user data.', true);
                });
        });
    });

    /**
     * Edit User Form Submission
     */
    if (editUserForm) {
        editUserForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const userId = document.getElementById('editUserId').value;
            const resetPassword = document.getElementById('resetPassword').checked;

            const userData = {
                firstName: document.getElementById('editFirstName').value,
                lastName: document.getElementById('editLastName').value,
                email: document.getElementById('editEmail').value,
                role: parseInt(document.getElementById('editRole').value),
                resetPassword: resetPassword
            };

            if (resetPassword) {
                userData.newPassword = document.getElementById('newPassword').value;
            }

            fetch(`/api/UserAdmin/${userId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(userData)
            })
                .then(response => {
                    if (!response.ok) {
                        return response.text().then(text => { throw new Error(text) });
                    }
                    return response.json();
                })
                .then(data => {
                    // Hide edit modal and show success modal
                    editUserModal.hide();
                    editConfirmModal.show();
                })
                .catch(error => {
                    console.error('Error updating user:', error);
                    showStatusMessage(error.message || 'Failed to update user.', true);
                });
        });
    }

    // Edit confirmation button click
    document.getElementById('editConfirmBtn')?.addEventListener('click', function () {
        editConfirmModal.hide();
        window.location.reload();
    });

    /**
     * Toggle Password Reset Fields
     */
    if (resetPasswordCheckbox) {
        resetPasswordCheckbox.addEventListener('change', function () {
            const passwordGroup = document.querySelector('.password-reset-group');
            passwordGroup.style.display = this.checked ? 'block' : 'none';

            if (!this.checked) {
                document.getElementById('newPassword').value = '';
            }
        });
    }

    /**
     * Enable/Disable User
     */
    document.querySelectorAll('.btn-enable, .btn-disable').forEach(button => {
        button.addEventListener('click', function () {
            const userId = this.getAttribute('data-user-id');
            const isEnable = this.classList.contains('btn-enable');

            fetch(`/api/UserAdmin/toggle-status/${userId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ isActive: isEnable })
            })
                .then(response => {
                    if (!response.ok) {
                        return response.text().then(text => { throw new Error(text) });
                    }

                    // Set confirmation message based on the action
                    const statusMessage = isEnable ? 'User has been activated successfully.' : 'User has been deactivated successfully.';
                    document.getElementById('statusConfirmMessage').textContent = statusMessage;

                    // Show confirmation modal instead of immediate reload
                    statusConfirmModal.show();
                })
                .catch(error => {
                    console.error('Error toggling user status:', error);
                    showStatusMessage(error.message || 'Failed to update user status.', true);
                });
        });
    });

    // Status confirmation button click
    document.getElementById('statusConfirmBtn')?.addEventListener('click', function () {
        statusConfirmModal.hide();
        window.location.reload();
    });

    /**
     * Delete User
     */
    let userIdToDelete;

    document.querySelectorAll('.btn-delete').forEach(button => {
        button.addEventListener('click', function () {
            userIdToDelete = this.getAttribute('data-user-id');
            deleteUserModal.show();
        });
    });

    document.getElementById('confirmDeleteBtn')?.addEventListener('click', function () {
        if (!userIdToDelete) return;

        fetch(`/api/UserAdmin/${userIdToDelete}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
            .then(response => {
                if (!response.ok) {
                    return response.text().then(text => { throw new Error(text) });
                }

                // Hide delete modal and show confirmation modal
                deleteUserModal.hide();
                deleteConfirmModal.show();
            })
            .catch(error => {
                console.error('Error deleting user:', error);
                showStatusMessage(error.message || 'Failed to delete user.', true);
                deleteUserModal.hide();
            });
    });

    // Delete confirmation button click
    document.getElementById('deleteConfirmBtn')?.addEventListener('click', function () {
        deleteConfirmModal.hide();
        window.location.reload();
    });

    /**
     * Search Functionality
     */
    if (searchInput) {
        searchInput.addEventListener('input', filterUsers);
    }

    /**
     * Filter Functionality
     */
    function filterUsers() {
        const searchTerm = searchInput?.value.toLowerCase() || '';
        const roleValue = roleFilter?.value || '';
        const statusValue = statusFilter?.value || '';
        const rows = document.querySelectorAll('#usersTable tbody tr');

        rows.forEach(row => {
            const name = row.cells[1].textContent.toLowerCase();
            const email = row.cells[2].textContent.toLowerCase();
            const roleCell = row.cells[3].textContent.trim();
            const statusCell = row.cells[4].textContent.trim();

            // Search match
            const searchMatch = !searchTerm ||
                name.includes(searchTerm) ||
                email.includes(searchTerm);

            // Role match
            let roleMatch = true;
            if (roleValue) {
                if (roleValue === '1' && roleCell.toLowerCase().includes('employee')) {
                    roleMatch = true;
                } else if (roleValue === '2' && roleCell.toLowerCase().includes('manager')) {
                    roleMatch = true;
                } else if (roleValue === '3' && roleCell.toLowerCase().includes('administrator')) {
                    roleMatch = true;
                } else {
                    roleMatch = false;
                }
            }

            // Status match
            let statusMatch = true;
            if (statusValue) {
                statusMatch = (statusValue === 'true' && statusCell === 'Active') ||
                    (statusValue === 'false' && statusCell === 'Inactive');
            }

            // Show/hide the row
            row.style.display = (searchMatch && roleMatch && statusMatch) ? '' : 'none';
        });
    }

    if (roleFilter) roleFilter.addEventListener('change', filterUsers);
    if (statusFilter) statusFilter.addEventListener('change', filterUsers);

    /**
     * Items Per Page Change
     */
    if (itemsPerPage) {
        itemsPerPage.addEventListener('change', function () {
            window.location.href = `/Admin/Users?page=1&pageSize=${this.value}`;
        });
    }

    /**
     * Select All Users Checkbox
     */
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            document.querySelectorAll('.user-checkbox').forEach(checkbox => {
                checkbox.checked = this.checked;
            });
        });
    }

    /**
     * Bulk Actions
     */
    if (applyBulkActionBtn) {
        applyBulkActionBtn.addEventListener('click', function () {
            const action = bulkActionSelect.value;

            if (!action) {
                showStatusMessage('Please select an action.', true);
                return;
            }

            const selectedUsers = Array.from(
                document.querySelectorAll('.user-checkbox:checked')
            ).map(cb => cb.value);

            if (selectedUsers.length === 0) {
                showStatusMessage('Please select at least one user.', true);
                return;
            }

            let role = null;
            if (action === 'changeRole') {
                role = prompt('Enter role (1 for Employee, 2 for Manager, 3 for Administrator):');
                if (!role || !['1', '2', '3'].includes(role)) {
                    showStatusMessage('Invalid role. Please enter 1, 2, or 3.', true);
                    return;
                }
            }

            if (action === 'delete') {
                if (!confirm('Are you sure you want to delete the selected users? This action cannot be undone.')) {
                    return;
                }
            }

            fetch('/api/UserAdmin/bulk-action', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    userIds: selectedUsers,
                    action: action,
                    role: role
                })
            })
                .then(response => {
                    if (!response.ok) {
                        return response.text().then(text => { throw new Error(text) });
                    }
                    return response.json();
                })
                .then(data => {
                    // Show confirmation modal instead of immediate reload
                    bulkConfirmModal.show();
                })
                .catch(error => {
                    console.error('Error performing bulk action:', error);
                    showStatusMessage(error.message || 'Failed to perform bulk action.', true);
                });
        });
    }

    // Bulk action confirmation button click
    document.getElementById('bulkConfirmBtn')?.addEventListener('click', function () {
        bulkConfirmModal.hide();
        window.location.reload();
    });

    /**
     * Export Users
     */
    if (exportBtn) {
        exportBtn.addEventListener('click', function () {
            window.location.href = '/Admin/Users?handler=ExportUsers';
        });
    }
});