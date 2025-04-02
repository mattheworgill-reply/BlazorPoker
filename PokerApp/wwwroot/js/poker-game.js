// Poker Game JavaScript Functions

// Timer functions
function startTimer(durationSeconds, displayElement, onTimeUpCallback) {
    let timer = durationSeconds;
    displayElement.textContent = timer;
    
    const interval = setInterval(() => {
        timer--;
        
        if (timer < 0) {
            clearInterval(interval);
            if (typeof onTimeUpCallback === 'function') {
                onTimeUpCallback();
            }
            return;
        }
        
        displayElement.textContent = timer;
        
        // Add visual indication when time is low
        if (timer <= 5) {
            displayElement.classList.add('time-warning');
        }
    }, 1000);
    
    // Return a function to cancel the timer
    return function cancelTimer() {
        clearInterval(interval);
    };
}

// Card animation
function animateCard(cardElement, fromX, fromY, toX, toY, onComplete) {
    cardElement.style.transition = 'none';
    cardElement.style.transform = `translate(${fromX}px, ${fromY}px)`;
    
    // Force reflow
    cardElement.offsetHeight;
    
    cardElement.style.transition = 'transform 0.5s ease-out';
    cardElement.style.transform = `translate(${toX}px, ${toY}px)`;
    
    cardElement.addEventListener('transitionend', function handler() {
        cardElement.removeEventListener('transitionend', handler);
        if (typeof onComplete === 'function') {
            onComplete();
        }
    });
}

// Chip animation
function animateChips(betAmount, fromElement, toElement, onComplete) {
    const chipContainer = document.createElement('div');
    chipContainer.className = 'chip-animation-container';
    document.body.appendChild(chipContainer);
    
    const fromRect = fromElement.getBoundingClientRect();
    const toRect = toElement.getBoundingClientRect();
    
    const fromX = fromRect.left + fromRect.width / 2;
    const fromY = fromRect.top + fromRect.height / 2;
    const toX = toRect.left + toRect.width / 2;
    const toY = toRect.top + toRect.height / 2;
    
    // Create chips based on bet amount
    const chipCount = Math.min(5, Math.max(1, Math.ceil(betAmount / 20)));
    
    for (let i = 0; i < chipCount; i++) {
        const chip = document.createElement('div');
        chip.className = 'animated-chip';
        
        // Random starting position near the player
        const randOffsetX = (Math.random() - 0.5) * 40;
        const randOffsetY = (Math.random() - 0.5) * 40;
        
        chip.style.left = `${fromX + randOffsetX}px`;
        chip.style.top = `${fromY + randOffsetY}px`;
        
        chipContainer.appendChild(chip);
        
        // Add slight delay for each chip
        setTimeout(() => {
            chip.style.left = `${toX}px`;
            chip.style.top = `${toY}px`;
            
            // Remove chip after animation completes
            chip.addEventListener('transitionend', function() {
                chip.remove();
                
                // If this is the last chip, call onComplete and remove container
                if (chipContainer.children.length === 0) {
                    chipContainer.remove();
                    if (typeof onComplete === 'function') {
                        onComplete();
                    }
                }
            });
        }, i * 100);
    }
}

// Connect SignalR functions
function setupSignalR(hubConnection, gameUpdatedCallback) {
    hubConnection.on('GameUpdate', (snapshot) => {
        if (typeof gameUpdatedCallback === 'function') {
            gameUpdatedCallback(snapshot);
        }
    });
}