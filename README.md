# 🚀 Reinforcement Learning for Earth–Moon Orbital Transfer

This project implements a simplified Earth–Moon orbital transfer environment using **Unity ML-Agents** and **Deep Reinforcement Learning (DRL)**.

The objective is to train an autonomous spacecraft capable of:

- Maintaining a stable Earth orbit
- Detecting an appropriate transfer window
- Performing translunar maneuvers
- Approaching the Moon under simplified orbital dynamics

The project was developed as part of doctoral-level research focused on Reinforcement Learning, orbital control, and intelligent autonomous navigation systems.

---

# 🧠 Reinforcement Learning Approach

The environment uses:

- **Unity ML-Agents**
- **Proximal Policy Optimization (PPO)**
- **Actor-Critic neural architecture**

The spacecraft agent learns through interaction with the environment using reward shaping and orbital constraints.

---

# 🌍 Environment Overview

The simulation contains three main entities:

- Earth
- Moon
- Spacecraft agent

The Moon follows a simplified orbit around Earth while the spacecraft learns orbital transfer behavior.

The simulation combines:

- Analytical orbital initialization
- Simplified gravity physics
- Reinforcement learning-based control

---

# 🛰 Mission Phases

The learning process was divided into sequential phases.

## Phase 1 — Stable Earth Orbit

The spacecraft starts with analytically calculated orbital velocity:

```math
v = \sqrt{\frac{\mu}{r}}
```

This allows the agent to begin from a physically stable orbit instead of learning gravity from scratch.

---

## Phase 2 — Transfer Window Detection

The agent must:

- complete at least 360° around Earth
- wait for an appropriate lunar transfer window
- begin translunar maneuvering

The transfer window depends on the relative angular position between the spacecraft and the Moon.

---

## Phase 3 — Randomized Generalization

To avoid memorization:

- Moon initial phase is randomized
- Spacecraft initial orbital position is randomized

This forces the agent to learn generalized orbital transfer policies.

---

# 🧠 Markov Decision Process (MDP)

The problem was formulated as a Markov Decision Process:

```math
\mathcal{M} = (\mathcal{S}, \mathcal{A}, P, R, \gamma)
```

Where:

- $\mathcal{S}$ → state space
- $\mathcal{A}$ → action space
- $P$ → transition dynamics
- $R$ → reward function
- $\gamma$ → discount factor

The transition dynamics were not explicitly modeled analytically. Instead, they were implicitly defined by Unity’s physics engine and the orbital mechanics scripts implemented in the environment.

The objective was to learn a policy:

```math
\pi_\theta(a_t \mid s_t)
```

that maximizes the expected return:

```math
J(\theta) =
\mathbb{E}_{\pi_\theta}
\left[
\sum_{t=0}^{T} \gamma^t r_t
\right]
```

---

# 🧠 Bellman Equation and Value Function

Although PPO does not directly solve the Bellman equation in tabular form, the learning process is still based on value estimation.

The state-value function is:

```math
V^\pi(s_t) =
\mathbb{E}_{\pi}
\left[
\sum_{k=0}^{T-t}
\gamma^k r_{t+k}
\mid s_t
\right]
```

which is related to the Bellman expectation equation:

```math
V^\pi(s_t) =
\mathbb{E}_{\pi}
\left[
r_t + \gamma V^\pi(s_{t+1})
\mid s_t
\right]
```

The PPO critic network estimates this value function during training.

---

# ❌ Why SARSA or Q-Learning Were Not Used

SARSA and Q-Learning were not used because:

- the environment contains continuous state spaces
- orbital dynamics generate delayed rewards
- the state dimensionality is too large for tabular methods

Traditional SARSA update:

```math
Q(s_t,a_t) \leftarrow Q(s_t,a_t) +
\alpha
\left[
r_t + \gamma Q(s_{t+1},a_{t+1}) - Q(s_t,a_t)
\right]
```

Traditional Q-Learning update:

```math
Q(s_t,a_t) \leftarrow Q(s_t,a_t) +
\alpha
\left[
r_t + \gamma \max_a Q(s_{t+1},a) - Q(s_t,a_t)
\right]
```

Instead, PPO directly optimizes a neural policy using policy gradients and value estimation.

---

# 🧠 PPO Optimization

The PPO algorithm uses a clipped objective function:

```math
L^{CLIP}(\theta) =
\mathbb{E}_t
\left[
\min
\left(
r_t(\theta) A_t,
\text{clip}(r_t(\theta), 1-\epsilon, 1+\epsilon) A_t
\right)
\right]
```

where:

```math
r_t(\theta) =
\frac{
\pi_\theta(a_t \mid s_t)
}{
\pi_{\theta_{old}}(a_t \mid s_t)
}
```

This clipping mechanism stabilizes policy updates and prevents catastrophic changes in orbital behavior.

---

# 🧠 Neural Network Architecture

The PPO agent uses an Actor-Critic architecture.

## Observation Space

The agent observes:

- Relative position to Earth
- Relative position to Moon
- Ship velocity
- Ship orientation
- Orbital progress
- Mission phase
- Relative angular alignment
- Moon velocity

Final observation vector size:

```text
16 observations
```

---

## Action Space

Discrete action branches:

### Rotation

```text
0 → No rotation
1 → Rotate left
2 → Rotate right
```

### Thrust

```text
0 → No thrust
1 → Forward thrust
2 → Reverse thrust
```

---

# 🧠 PPO Hyperparameters

```yaml
behaviors:
  ShipBehavior:
    trainer_type: ppo

    hyperparameters:
      batch_size: 1024
      buffer_size: 20480
      learning_rate: 7.0e-5
      beta: 1.0e-3
      epsilon: 0.08
      lambd: 0.95
      num_epoch: 2

    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 3

    reward_signals:
      extrinsic:
        gamma: 0.997
        strength: 1.0

    max_steps: 400000
    time_horizon: 256
```

---

# 🧠 Neural Network Parameterization

| Component | Configuration |
|---|---|
| Algorithm | PPO |
| Architecture | Actor-Critic |
| Hidden Units | 256 |
| Hidden Layers | 3 |
| Observation Size | 16 |
| Stacked Vectors | 1 |
| Action Type | Discrete |
| Action Branches | 2 |

---

# 🎯 Reward Shaping

The reward function combines dense and sparse rewards.

## Positive Rewards

- Orbital progress
- Correct transfer timing
- Reducing Moon distance
- Entering Moon vicinity
- Successful Moon reach

## Penalties

- Step penalty
- Crashing into Earth
- Leaving the Earth–Moon system
- Missing transfer windows
- Excessive velocity

---

# 🌌 System Boundary

The system boundary was defined as:

```math
d_{ship,Earth} >
\alpha \cdot d_{Earth,Moon}
```

where:

```text
α = 1.5
```

If the spacecraft exceeded this boundary, the episode terminated with a penalty.

---

# 📈 Training Results

Representative results:

| Experiment | Mean Reward | Max Reward |
|---|---|---|
| Initial Transfer | 5.26 | 7.37 |
| Randomized Moon Phase | 7.39 | 9.18 |
| Stable PPO Configuration | 8.13 | 9.70 |

The best models successfully learned:

- Stable Earth orbital behavior
- Full orbital revolutions
- Transfer window timing
- Translunar trajectory generation

---

# 🔬 Important Findings

Several key observations emerged during experimentation:

- Physics-based orbital initialization was critical
- PPO was significantly more stable than attempting pure orbital discovery
- Reward shaping strongly influenced trajectory quality
- Numerical reward increases did not always imply visually correct trajectories
- Environment randomization improved policy generalization

---

# 🛠 Technologies Used

- Unity
- Unity ML-Agents
- Python
- PyTorch
- PPO (Proximal Policy Optimization)

---

# ▶️ Training

Example training command:

```bash
mlagents-learn ship_orbit_moon.yaml --run-id=phase3_stable_v1 --force
```

---

# 📚 References

- Sutton, R. & Barto, A. *Reinforcement Learning: An Introduction*
- Schulman et al. *Proximal Policy Optimization Algorithms*
- Unity ML-Agents Documentation
- Curtis, H. *Orbital Mechanics for Engineering Students*

---

# 👨‍💻 Author

Guillermo Campos Lozano  
PhD(c) in Artificial Intelligence  
Director — AI Factory