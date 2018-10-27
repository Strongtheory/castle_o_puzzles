﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PhysicsPropHandler : MonoBehaviour 
{
    private Dictionary<Type, PhysicsPlugin> plugins;

    private void Awake() {
        plugins = new Dictionary<Type, PhysicsPlugin>() {
            {typeof(Pushable), new Pusher(context: this)}
        };

        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Awake();
        }
    }

    private void Start() {
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Start();
        }
    }

    private void Update() {
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Update();
        }
    }

    private void FixedUpdate() {
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.FixedUpdate();
        }
    }

    private void OnTriggerEnter(Collider other) {
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerEnter(other, prop);
        }
    }

    private void OnTriggerStay(Collider other) {
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerStay(other, prop);
        }
    }

    private void OnTriggerExit(Collider other) {
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerExit(other, prop);
        }
    }

    private void OnCollisionEnter(Collision other) {
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionEnter(other, prop);
        }
    }

    private void OnCollisionStay(Collision other) {
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionStay(other, prop);
        }
    }

    private void OnCollisionExit(Collision other) {
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionExit(other, prop);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        PhysicsProp[] props = hit.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnControllerColliderHit(hit, prop);
        }
    }
}