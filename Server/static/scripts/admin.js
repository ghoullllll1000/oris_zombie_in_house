(function () {
    const typeSelect = document.getElementById('type');
    const container = document.getElementById('dynamic-field-container');
    const label = document.getElementById('dynamic-field-label');
    const form = document.querySelector('.add-panel form');
    const description = document.getElementById('characteristics');
    const name = document.getElementById('name');

    const configs = {
        character: {
            label: 'Здоровье',
            name: 'health',
            values: ['3', '5']
        },
        monster: {
            label: 'Тип монстра',
            name: 'category',
            values: ['Обычный', 'Босс']
        },
        weapon: {
            label: 'Тип оружия',
            name: 'category',
            values: ['Холодное оружие', 'Огнестрельное оружие', 'Взрывчатка', 'Специальное оружие']
        },
        item: {
            label: 'Тип вещи',
            name: 'category',
            values: ['Здоровье', 'Защита проёмов', 'Машина']
        }
    };

    function updateField() {
        const config = configs[typeSelect.value];

        const oldField = container.querySelector('select');
        if (oldField) oldField.remove();

        if (!config) {
            container.style.display = 'none';
            return;
        }

        label.textContent = config.label;

        const select = document.createElement('select');
        select.name = config.name;
        select.id = 'dynamic-field-input';
        select.required = true;

        const emptyOption = document.createElement('option');
        emptyOption.value = '';
        emptyOption.textContent = '---';
        select.appendChild(emptyOption);

        config.values.forEach(function (value) {
            const option = document.createElement('option');
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        });

        container.appendChild(select);
        container.style.display = '';
    }

    form.addEventListener('submit', function (event) {
        const dynamicField = document.getElementById('dynamic-field-input');
        const name = document.getElementById('name');

        if (!typeSelect.value) {
            event.preventDefault();
            alert('Выберите тип!');
            return;
        }
        if (!dynamicField || !dynamicField.value) {
            event.preventDefault();
            alert('Заполните поле!');
            return;
        }
        if (!description.value.trim()) {  
            event.preventDefault();
            alert('Заполните описание!');
            return;
        }

        if (!name.value.trim()) {
            event.preventDefault();
            alert('Введите имя!');
            return;
        }

        if (name.value.trim().length < 2 || name.value.trim().length > 100) {
            event.preventDefault();
            alert('Имя должно содержать от 2 до 100 символов!');
            return;
        }

        if (!/^[а-яА-ЯёЁa-zA-Z0-9 -]+$/.test(name.value.trim())) {
            event.preventDefault();
            alert('Имя содержит недопустимые символы!');
            return;
        }
    });

    document.addEventListener('submit', async function (event) {
        const deleteForm = event.target.closest('.delete-form');

        if (!deleteForm) {
            return;
        }

        event.preventDefault();

        try {
            const formData = new FormData(deleteForm);

            const response = await fetch(deleteForm.action, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: new URLSearchParams(formData)
            });

            const result = await response.json();

            if (result.success) {
                deleteForm.closest('tr').remove();
            } else {
                alert(result.message);
            }
        } catch (error) {
            alert('Не удалось удалить запись. Попробуйте ещё раз.');
            console.error(error);
        }
    });

    typeSelect.addEventListener('change', updateField);
    updateField();
})();