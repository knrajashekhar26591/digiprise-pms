// ===== GLOBAL STATE =====
let token = null;
let currentProject = null;
let currentIssueId = null;

const API = "http://localhost:5000/api/v1";

// ===== LOGIN =====
async function doLogin() {
  const email = document.getElementById("le").value;
  const password = document.getElementById("lp").value;

  const res = await fetch(`${API}/auth/login?tenantId=1`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  if (!res.ok) {
    document.getElementById("lerr").innerText = "Login failed";
    return;
  }

  const data = await res.json();
  token = data.accessToken;

  document.getElementById("login-screen").style.display = "none";
  document.getElementById("app").classList.add("on");

  loadProjects();
}

// ===== PROJECTS =====
async function loadProjects() {
  const res = await fetch(`${API}/projects`, {
    headers: { Authorization: `Bearer ${token}` }
  });

  const projects = await res.json();
  const container = document.getElementById("pp-grid");
  container.innerHTML = "";

  projects.forEach(p => {
    const div = document.createElement("div");
    div.className = "pp-card";
    div.innerHTML = `<b>${p.name}</b><br>${p.key}`;
    div.onclick = () => selectProject(p);
    container.appendChild(div);
  });
}

// ===== SELECT PROJECT =====
async function selectProject(p) {
  currentProject = p;
  document.getElementById("ph-name").innerText = p.name;

  const res = await fetch(`${API}/issues/project/${p.id}`, {
    headers: { Authorization: `Bearer ${token}` }
  });

  const issues = await res.json();
  renderBacklog(issues);
}

// ===== RENDER BACKLOG =====
function renderBacklog(issues) {
  const container = document.getElementById("bl-content");
  container.innerHTML = "";

  issues.forEach(i => {
    const row = document.createElement("div");
    row.className = "ir";
    row.dataset.id = i.id;

    row.innerHTML = `
      <div class="ir-key">${i.key}</div>
      <div class="ir-sum">${i.summary}</div>
    `;

    row.onclick = () => openIssue(i);
    row.draggable = true;

    row.ondragstart = (e) => {
      e.dataTransfer.setData("issueId", i.id);
    };

    container.appendChild(row);
  });
}

// ===== CREATE ISSUE =====
function openCreateIssue() {
  document.getElementById("modal-bg").classList.add("on");
}

function closeModal() {
  document.getElementById("modal-bg").classList.remove("on");
}

async function createIssue() {
  const summary = document.getElementById("ci-summary").value;

  const res = await fetch(`${API}/issues`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify({
      projectKey: currentProject.key,
      summary: summary,
      issueType: "Story",
      priority: "Major"
    })
  });

  if (!res.ok) {
    alert("Failed to create issue");
    return;
  }

  closeModal();
  selectProject(currentProject);
}

// ===== ISSUE DETAILS =====
function openIssue(issue) {
  currentIssueId = issue.id;

  document.getElementById("dp").classList.add("on");
  document.getElementById("dp-title").value = issue.summary;
  document.getElementById("dp-key").innerText = issue.key;
  document.getElementById("dp-desc").value = issue.description || "";
}

function closeDP() {
  document.getElementById("dp").classList.remove("on");
}

// ===== UPDATE DESCRIPTION =====
async function saveDesc() {
  const desc = document.getElementById("dp-desc").value;

  await fetch(`${API}/issues/${currentIssueId}`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ description: desc })
  });
}

// ===== STATUS CHANGE =====
async function setStatus(id, name) {
  await fetch(`${API}/issues/${currentIssueId}/transitions`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ status: name })
  });

  closeDP();
  selectProject(currentProject);
}

// ===== DRAG → SPRINT =====
function allowDrop(ev) {
  ev.preventDefault();
}

function dropToSprint(ev, sprintId) {
  ev.preventDefault();
  const issueId = ev.dataTransfer.getData("issueId");

  fetch(`${API}/issues/${issueId}`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ sprintId })
  }).then(() => selectProject(currentProject));
}
