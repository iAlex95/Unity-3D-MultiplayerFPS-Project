﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[RequireComponent(typeof(PlayerSetup))]
public class Player : NetworkBehaviour {

	[SyncVar] private bool _isDead = false;
	public bool isDead {
		get { return _isDead; }
		protected set { _isDead = value; }
	}

	[SerializeField] private int maxHealth = 100;

	[SyncVar] private int currentHealth;

	public float GetHealthAmount() {
		return (float) currentHealth/maxHealth;
	}

	[SyncVar] public string username = "Loading...";

	public int kills;
	public int deaths;

	[SerializeField] private Behaviour[] disableOnDeath;
	private bool[] wasEnabled;

	[SerializeField] private GameObject deathEffect;

	[SerializeField] private GameObject[] disableGameObjectsOnDeath;

	[SerializeField] private GameObject spawnEffect;

	private bool firstSetup = true;

	public void SetupPlayer() {
		if (isLocalPlayer) {
			GameManager.instance.SetSceneCameraActive(false);
			GetComponent<PlayerSetup>().playerUIToDisable.SetActive(true);
		}

		CmdBroadcastNewSetupPlayer();
	}

	[Command]
	private void CmdBroadcastNewSetupPlayer() {
		RpcSetupPlayerOnAllClients();
	}

	[ClientRpc]
	private void RpcSetupPlayerOnAllClients() {
		if (firstSetup) {
			wasEnabled = new bool[disableOnDeath.Length];
			for (int i = 0; i < wasEnabled.Length; i++) {
				wasEnabled[i] = disableOnDeath[i].enabled;
			}

			firstSetup = false;
		}

		SetDefaults();
	}

	void Update() {
		if (!isLocalPlayer) return;
	
		if (Input.GetKeyDown(KeyCode.K)) {
			RpcTakeDamage(10, username);
		}

		/*if (PauseMenu.isOn) {
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		} else {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}*/
	}

	[ClientRpc]
	public void RpcTakeDamage(int _amount, string _sourceID) {
		if (isDead) return;

		currentHealth -= _amount;

		Debug.Log(transform.name + " now has " + currentHealth + " health.");
	
		if (currentHealth <= 0) {
			Die(_sourceID);
		}
	}

	private void Die(string _sourceID) {
		isDead = true;

		Player sourcePlayer = GameManager.GetPlayer(_sourceID);
		if (sourcePlayer != null) {
			sourcePlayer.kills++;
			GameManager.instance.onPlayerKilledCallback.Invoke(username, sourcePlayer.username);
		}


		deaths++;

		for (int i = 0; i < disableOnDeath.Length; i++) {
			disableOnDeath[i].enabled = false;
		}
		for (int i = 0; i < disableGameObjectsOnDeath.Length; i++) {
			disableGameObjectsOnDeath[i].SetActive(false);
		}

		Collider _col = GetComponent<Collider>();
		if (_col != null) {
			_col.enabled = false;
		}

		GameObject _gfxIns = (GameObject) Instantiate(deathEffect, transform.position, Quaternion.identity);
		Destroy(_gfxIns, 3f);

		if (isLocalPlayer) {
			GameManager.instance.SetSceneCameraActive(true);
			GetComponent<PlayerSetup>().playerUIToDisable.SetActive(false);
		}
		
		Debug.Log(transform.name + " is dead.");

		// CALL RESPAWN METHOD
		StartCoroutine(Respawn());
	}

	private IEnumerator Respawn() {
		yield return new WaitForSeconds(GameManager.instance.matchSettings.respawnTime);

		Transform _spawnPoint = NetworkManager.singleton.GetStartPosition();
		transform.position = _spawnPoint.position;
		transform.rotation = _spawnPoint.rotation;

		yield return new WaitForSeconds(0.25f);

		SetupPlayer();

		Debug.Log(transform.name + " has respawned.");
	}

	public void SetDefaults() {
		isDead = false;

		currentHealth = maxHealth;
	
		for (int i = 0; i < disableOnDeath.Length; i++) {
			disableOnDeath[i].enabled = wasEnabled[i];
		}
		for (int i = 0; i < disableGameObjectsOnDeath.Length; i++) {
			disableGameObjectsOnDeath[i].SetActive(true);
		}

		Collider _col = GetComponent<Collider>();
		if (_col != null) {
			_col.enabled = true;
		}

		GameObject _gfxIns = (GameObject) Instantiate (spawnEffect, transform.position, Quaternion.identity);
		Destroy(_gfxIns, 3f);
	}

}
